# TerraTech en Cloud Run

El contenedor mantiene .NET 10, EF Core y MySQL. Cloud Run aloja la API; la base debe ser externa y persistente. Cloud SQL MySQL es una opción, no un recurso creado automáticamente por este repositorio. Cambiar a PostgreSQL/AlloyDB requeriría otra implementación del proveedor y migraciones.


## PostgreSQL en Supabase

La configuración actual de PostgreSQL se describe en [Supabase](SUPABASE.md).
Seleccionar `Database__Provider=PostgreSql` y ejecutar el Job migrador antes de
la API con `Database__MigrateOnStartup=false`. Las migraciones PostgreSQL usan
un contexto separado y el esquema `terratech`. La configuración MySQL de este
documento corresponde al proveedor anterior.

El workflow de GitHub Actions fue retirado. La construcción manual se invoca
con `cloudbuild.manual.yaml`. Los triggers de Cloud Build con configuración
inline se administran por separado desde GCP.

## Comportamiento del servicio

- Imagen publicada exclusivamente en **Debug**, para `linux/amd64`, con usuario no root.
- Escucha HTTP en `0.0.0.0:$PORT` (8080 por defecto en el Dockerfile). Un puerto inválido detiene el proceso.
- Cloud Run termina TLS. No se redirige el HTTP interno a HTTPS. En Cloud Run (`K_SERVICE`), solo se procesa `X-Forwarded-Proto` para construir URLs HTTPS; no se confía en hosts ni IPs reenviados.
- Con `Database__MigrateOnStartup=true`, también en Cloud Run/Production, se crea la base si falta, se comprueban los datos existentes y se aplican las migraciones antes de servir HTTP. Un bloqueo MySQL por nombre de base serializa arranques concurrentes. Con `false`, se usa un Job explícito con `--migrate-only`. El valor predeterminado en Production sigue siendo `false`.
- `/health/live`: proceso disponible, sin depender de la base. `/health/ready`: conexión MySQL y ausencia de migraciones pendientes; devuelve 503 si falla. Son anónimos y no exponen diagnósticos internos.
- Swagger y registro/login siguen públicos; el resto mantiene JWT y propiedad. El acceso público de Cloud Run permite llegar a estos endpoints; no sustituye la autorización de la API.
- Sin persistencia en disco local ni datos de demostración automáticos. Los comandos demo siguen bloqueados en Production.

## Requisitos antes de desplegar

Elegir proyecto, región, base MySQL y cuenta de servicio. Habilitar Cloud Run, Cloud Build, Artifact Registry, Secret Manager y, si corresponde, Cloud SQL Admin. Crear el repositorio Docker de Artifact Registry. Estos pasos y los comandos siguientes pueden generar cargos; aquí se documentan, no se ejecutan automáticamente.

Para la creación automática, el usuario de la conexión de la API necesita permisos para crear la base y aplicar DDL, además de leer/escribir los datos. El nombre de base (por ejemplo `terratech` o `defaultdb` en Aiven) debe estar definido aunque la base aún no exista. No dejar la cadena de conexión vacía. Si se elige un Job separado, crear dos usuarios MySQL: migrador con permisos DDL y runtime con permisos de lectura/escritura sobre la base y lectura de `__EFMigrationsHistory`. Respaldar cualquier base existente. No ejecutar dos migradores simultáneos ni migrar mientras otras revisiones escriben durante cambios incompatibles.

Guardar en Secret Manager:

| Secreto de ejemplo | Valor |
| --- | --- |
| `terratech-jwt` | Clave aleatoria de al menos 32 bytes UTF-8, igual en todas las instancias |
| `terratech-db-runtime` | Conexión MySQL con el usuario runtime |
| `terratech-db-migrator` | Conexión MySQL con el usuario migrador |

La cuenta runtime necesita `roles/secretmanager.secretAccessor` solo sobre JWT y su conexión; la migradora, sobre JWT y su conexión. Ambas necesitan `roles/cloudsql.client` si se usa la integración Cloud SQL. La identidad de Cloud Build necesita escritura en Artifact Registry. La identidad que despliega necesita permisos Cloud Run y actuar como las cuentas correspondientes.

Para Cloud SQL con socket integrado, el contenido de cada secreto de conexión puede seguir este formato (sustituir todos los valores):

```text
Server=/cloudsql/PROJECT_ID:REGION:INSTANCE;Protocol=unix;Database=terratech;User ID=DB_USER;Password=DB_PASSWORD;SslMode=Disabled;Pooling=true;MinimumPoolSize=0;MaximumPoolSize=10;ConnectionTimeout=10;DefaultCommandTimeout=30
```

El socket usa el proxy autenticado de Cloud SQL, que cifra el tramo hacia la instancia. `SslMode=Disabled` aplica solo a este socket/proxy, no a una conexión TCP pública. Para MySQL por TCP usar la red privada/VPC o TLS verificado según el proveedor. No agregar credenciales al YAML, Dockerfile, argumentos de build ni Git. Para contraseñas con caracteres especiales, generar la cadena mediante `MySqlConnectionStringBuilder` en lugar de concatenarla.

Referencia: [Cloud Run → Cloud SQL MySQL](https://docs.cloud.google.com/sql/docs/mysql/connect-run), [socket con Connector/NET](https://docs.cloud.google.com/sql/docs/mysql/samples/cloud-sql-mysql-dotnet-ado-connect-unix).

## Construir la imagen

Desde la raíz del backend, completar estos valores no secretos:

```sh
export GCP_PROJECT='TU_PROJECT_ID'
export GCP_REGION='us-east1'
export API_SERVICE='TU_SERVICIO'
export ARTIFACT_REPOSITORY='cloud-run-source-deploy'
export IMAGE_TAG="$(git rev-parse HEAD)"
export API_IMAGE="$GCP_REGION-docker.pkg.dev/$GCP_PROJECT/$ARTIFACT_REPOSITORY/terratech-api:$IMAGE_TAG"
export SQL_INSTANCE="$GCP_PROJECT:$GCP_REGION:TU_INSTANCIA_MYSQL"
export RUNTIME_SA="terratech-api@$GCP_PROJECT.iam.gserviceaccount.com"
export MIGRATOR_SA="terratech-migrator@$GCP_PROJECT.iam.gserviceaccount.com"
```

`cloudbuild.manual.yaml` construye y publica la imagen; **no despliega ni aplica migraciones**. Usa una etiqueta distinta por commit.

```sh
gcloud builds submit --project "$GCP_PROJECT" --config cloudbuild.manual.yaml \
  --substitutions="_REGION=$GCP_REGION,_REPOSITORY=$ARTIFACT_REPOSITORY,_TAG=$IMAGE_TAG" .
```

Alternativa local: `docker build --platform=linux/amd64 -t terratech-cloud-run:debug .`. No construir solo ARM64 desde un Mac: Cloud Run requiere una imagen compatible con AMD64.

## Migrar con un Job separado

Ejecutar las migraciones explícitamente antes de desplegar la API. Estos ejemplos de Job usan Cloud SQL. Para Aiven, quitar los flags `--set-cloudsql-instances`/`--add-cloudsql-instances` y usar una conexión TCP válida para el servicio y el Job.

Se fijan versiones numéricas de secretos (ejemplo `1`); sustituirlas por las versiones válidas. No usar `latest` para una revisión que deba ser reproducible.

```sh
gcloud run jobs deploy terratech-migrate --project "$GCP_PROJECT" --region "$GCP_REGION" \
  --image "$API_IMAGE" --service-account "$MIGRATOR_SA" \
  --args=--migrate-only --tasks=1 --parallelism=1 --max-retries=0 --task-timeout=10m \
  --cpu=1 --memory=512Mi \
  --set-env-vars=ASPNETCORE_ENVIRONMENT=Production \
  --set-secrets=TokenSettings__Secret=terratech-jwt:1,ConnectionStrings__DefaultConnection=terratech-db-migrator:1 \
  --set-cloudsql-instances "$SQL_INSTANCE"

gcloud run jobs execute terratech-migrate --project "$GCP_PROJECT" --region "$GCP_REGION" --wait
```

Continuar solo si el Job termina correctamente. El preflight conserva los diagnósticos de huérfanos/duplicados y no elimina información automáticamente. Si falla, revisar logs y resolver los datos antes de reintentar. Las restricciones de MySQL y el respaldo siguen siendo necesarios.

## Desplegar la API

Configuración inicial para TB1: mínimo 0, máximo 1, concurrencia 10, 1 CPU y 512 MiB. Estos límites reducen exposición de costo y conexiones, pero no garantizan una factura de cero. Las revisiones antiguas y nuevas pueden coexistir durante un despliegue.

```sh
gcloud run deploy "$API_SERVICE" --project "$GCP_PROJECT" --region "$GCP_REGION" \
  --image "$API_IMAGE" --service-account "$RUNTIME_SA" --execution-environment=gen2 \
  --allow-unauthenticated --port=8080 --cpu=1 --memory=512Mi \
  --min=0 --max=1 --concurrency=10 --timeout=60s \
  --set-env-vars=ASPNETCORE_ENVIRONMENT=Production,Database__MigrateOnStartup=false \
  --set-secrets=TokenSettings__Secret=terratech-jwt:1,ConnectionStrings__DefaultConnection=terratech-db-runtime:1 \
  --add-cloudsql-instances "$SQL_INSTANCE" \
  --startup-probe='httpGet.path=/health/ready,httpGet.port=8080,timeoutSeconds=5,periodSeconds=10,failureThreshold=12' \
  --liveness-probe='httpGet.path=/health/live,httpGet.port=8080,timeoutSeconds=5,periodSeconds=30,failureThreshold=3'
```

Con creación automática, `terratech-db-runtime` debe contener un usuario con permisos DDL. Si ya ejecutaste el Job con un usuario separado, configura `Database__MigrateOnStartup=false` para mantener el usuario runtime limitado.

La prueba de arranque verifica base y esquema; la de liveness evita reiniciar por una caída transitoria de MySQL. `/health/ready` queda disponible para monitoreo posterior.

Obtener la URL HTTPS y comprobar:

```sh
export API_URL="$(gcloud run services describe "$API_SERVICE" --project "$GCP_PROJECT" --region "$GCP_REGION" --format='value(status.url)')"
curl --fail "$API_URL/health/live"
curl --fail "$API_URL/health/ready"
curl --fail "$API_URL/swagger/v1/swagger.json"
```

Ejecutar después [TB1.http](TB1.http) usando una cuenta temporal y esta URL; las lecturas simuladas deben haberse provisionado explícitamente antes, mediante un proceso de demostración en Development y una transferencia de datos revisada. No habilitar comandos demo en el servicio Production. Android deberá usar `$API_URL/` como URL base HTTPS; configurar Android queda fuera de esta adaptación.

Si se necesita revertir, devolver tráfico a una revisión anterior compatible con el esquema. No aplicar automáticamente migraciones Down. Exportar datos antes de eliminar una prueba gratuita de base de datos.

Documentación oficial: [contrato del contenedor](https://docs.cloud.google.com/run/docs/container-contract), [Secret Manager](https://docs.cloud.google.com/run/docs/configuring/services/secrets), [Cloud Run Jobs](https://docs.cloud.google.com/run/docs/create-jobs).
