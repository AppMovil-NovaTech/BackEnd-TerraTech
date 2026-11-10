# PostgreSQL en Supabase

El backend usa Npgsql con EF Core 10. Seleccionar el proveedor explícitamente.
La autenticación de la API sigue usando sus endpoints IAM y su clave JWT.

## Conexión

Copiar la conexión desde Connect en el proyecto de Supabase. En redes IPv4,
usar Session pooler, puerto 5432, con el usuario `postgres.PROJECT_REF`.
La conexión directa `db.PROJECT_REF.supabase.co` requiere conectividad IPv6
o el complemento IPv4 del proveedor.

Guardar la cadena completa en Secret Manager y exponerla como
`ConnectionStrings__DefaultConnection`. Se admite una URI `postgresql://`
con credenciales percent-encoded o el formato Npgsql siguiente.

```text
Host=POOLER_HOST;Port=5432;Database=postgres;Username=postgres.PROJECT_REF;Password=DB_PASSWORD;SSL Mode=Require;Maximum Pool Size=10;Timeout=15;Command Timeout=30
```

`SSL Mode=Require` cifra la conexión. Para validar además certificado y hostname,
descargar el certificado CA de Database Settings, montarlo en el contenedor y
configurar `SSL Mode=VerifyFull;Root Certificate=/ruta/supabase-ca.crt`.
Las opciones avanzadas se pasan en formato Npgsql. Las URI admiten `sslmode`.

La API necesita estas variables y secretos.

| Nombre | Configuración |
| --- | --- |
| `Database__Provider` | `PostgreSql` |
| `Database__MigrateOnStartup` | `false` después de ejecutar el Job |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Referencia al secreto de la conexión |
| `TokenSettings__Secret` | Referencia al secreto JWT existente |
| `Email__ApiKey` | Referencia al secreto del proveedor de correo |
| `Email__From` | Remitente autorizado por el proveedor |
| `Email__PublicBaseUrl` | URL pública HTTPS de la API |

El registro necesita el servicio de correo configurado para enviar la
verificación. Revisar el dominio/remitente y las restricciones de entrega
de la cuenta de correo al probar con usuarios nuevos.

## Esquema y migraciones

`PostgreSqlAppDbContext` reutiliza el modelo de dominio y genera sus migraciones
en `Migrations/PostgreSql`. Crea las tablas y el historial EF en el esquema
`terratech`, separado de los esquemas administrados por Supabase. Mantener este
esquema fuera de los esquemas expuestos por Data API. Los errores únicos y de
claves foráneas mantienen las respuestas de conflicto de la API, y los
DateTimeOffset se normalizan a UTC al guardar.

Construir una imagen y usar esa misma imagen para el Job y la API. Configurar el
Job con `Database__Provider=PostgreSql`, los secretos de conexión y JWT, y el
argumento `--migrate-only`. Ejecutarlo con una tarea, paralelismo 1 y reintentos
0. Continuar cuando termine correctamente. El servicio web se despliega después
con `Database__MigrateOnStartup=false`.

En un entorno local con variables ya configuradas, el mismo comando es

```sh
dotnet run --project NovaTech.TerraTech.Platform --no-launch-profile -- --migrate-only
```

Para nuevas migraciones, indicar el contexto PostgreSQL explícitamente.

```sh
dotnet ef migrations add NOMBRE --context PostgreSqlAppDbContext \
  --project NovaTech.TerraTech.Platform --output-dir Migrations/PostgreSql
```

La migración inicial crea un esquema vacío. Trasladar los registros de MySQL
requiere una exportación y conversión aparte, con preservación de IDs,
relaciones y restablecimiento de secuencias. Respaldar la fuente antes del traslado.

## Construcción y despliegue manual

Desde la raíz del backend, completar las variables no secretas y construir.

```sh
export GCP_PROJECT='TU_PROJECT_ID'
export GCP_REGION='us-east1'
export ARTIFACT_REPOSITORY='cloud-run-source-deploy'
export IMAGE_TAG="$(git rev-parse HEAD)"
gcloud builds submit --project "$GCP_PROJECT" --config cloudbuild.manual.yaml \
  --substitutions="_REGION=$GCP_REGION,_REPOSITORY=$ARTIFACT_REPOSITORY,_TAG=$IMAGE_TAG" .
```

En Cloud Run, seleccionar la imagen construida, configurar las variables y
referencias a secretos indicadas, y ejecutar primero el Job migrador. Usar
`/health/ready` como startup probe y `/health/live` como liveness probe.
Comprobar readiness, Swagger, registro/verificación, login y lectura de parcelas
antes de asignar tráfico. Los triggers automáticos se administran en Cloud Build.

## Validación

```sh
dotnet test TerraTech.IntegrationTests --filter FullyQualifiedName~PostgreSqlConfigurationTests
export TB1_TEST_POSTGRES='Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=TEST_PASSWORD'
dotnet test TerraTech.IntegrationTests --filter FullyQualifiedName~PostgreSqlJourneyTests
```

El recorrido PostgreSQL necesita un servidor aislado con permiso CREATE DATABASE.
Crea una base `terratech_test_UUID` para verificar migraciones repetibles, salud,
Swagger, registro, verificación, login, perfil, parcela y dispositivo.
Las pruebas históricas MySQL requieren `TB1_TEST_MYSQL` y conservan sus migraciones.

Referencias oficiales

- [Conexiones de Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres)
- [TLS y certificados](https://supabase.com/docs/guides/platform/ssl-enforcement)
- [Proveedor Npgsql para EF Core](https://www.npgsql.org/efcore/)
