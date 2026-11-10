# TerraTech backend — TB1

.NET 10, ASP.NET Core, EF Core y MySQL. Se mantienen los contextos DDD, agregados, repositorios, servicios de aplicación y las rutas anteriores. Los nuevos servicios de cuenta, perfil propio y sensores usan los puertos de persistencia; la seguridad y las migraciones se centralizan en infraestructura compartida.

El recorrido de esta entrega es **registro con sesión inmediata → perfil → parcela → asociación de sensor → última lectura → histórico de 7/30 días**. Los datos de demostración llevan `source: "SIMULATED"`. Android compondrá sus pantallas y guardará estos resultados en Room; el servidor no requiere un dashboard agregado ni endpoints offline.

## Ejecutar en desarrollo

Requisitos: SDK .NET 10 y MySQL. Crear una base vacía de desarrollo y un usuario con permisos de migración. Las configuraciones versionadas no contienen credenciales. Definir localmente:

```sh
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection='server=localhost;port=3306;database=terratech_dev;user=terratech;password=TU_CLAVE_LOCAL'
export TokenSettings__Secret="$(openssl rand -base64 48)"
dotnet build NovaTech.TerraTech.Platform -c Debug
dotnet run --project NovaTech.TerraTech.Platform -c Debug --no-build --no-launch-profile --urls http://localhost:8080
```

Mantener la misma clave entre reinicios si se quiere conservar la validez de los tokens. JWT HS256, firma y expiración obligatorias, duración de ocho horas y validación del usuario existente. Una clave vacía o menor de 32 bytes impide iniciar. Swagger público: `/swagger/index.html`; contrato: `/swagger/v1/swagger.json`.

En Development, al iniciar se ejecuta primero el diagnóstico de datos existentes y luego las migraciones pendientes. En Production y Cloud Run, `Database__MigrateOnStartup=true` crea la base si falta y aplica las migraciones al arrancar. Con `false`, se usa el comando explícito `--migrate-only`. El nombre de base y las credenciales deben estar definidos en la conexión, con permisos de creación y migración. No se borra ni se recrea la base. Si existen referencias huérfanas o duplicados, el arranque se detiene con el diagnóstico. Ver [migraciones y despliegue](docs/DEPLOYMENT.md).

## Preparar la demostración

Con las mismas variables de entorno:

```sh
dotnet run --project NovaTech.TerraTech.Platform -c Debug --no-build --no-launch-profile -- --demo-catalog
```

Provisiona cinco sensores: `TT-ZZZ001` a `TT-ZZZ005`, MAC `02:54:54:00:00:01` a `02:54:54:00:00:05`. Es repetible: conserva el catálogo existente y detecta colisiones. No crea usuarios ni contraseñas.

1. Registrar una cuenta enviando `fullName`, `emailAddress`, `password` y `confirmPassword` idéntico; usar el token devuelto por el registro.
2. Completar `PUT /api/v1/profiles/me`.
3. Crear una parcela propia mediante `POST /api/v1/fields`.
4. Asociar `TT-ZZZ001` mediante `POST /api/v1/devices/register` y anotar el ID devuelto.
5. Ejecutar explícitamente:

```sh
dotnet run --project NovaTech.TerraTech.Platform -c Debug --no-build --no-launch-profile -- --demo-readings --device-id 1
```

Sustituir `1` por el ID real. Genera 720 lecturas horarias durante 30 días, exclusivamente para sensores de demostración ya asociados. Repetir en la misma hora no duplica registros; en otra hora completa las horas nuevas sin borrar las anteriores. La última muestra se fija al inicio de la hora UTC: `isStale` puede ser verdadero si ya pasaron 30 minutos, lo que demuestra el estado de datos antiguos. No inventa batería ni calidad de medición. Ambos comandos se bloquean antes de migrar o escribir datos fuera de `Development`, incluido `Production`.

Ejemplos completos: [TB1.http](docs/TB1.http). Contrato para Android: [ANDROID_CONTRACT.md](docs/ANDROID_CONTRACT.md).

## Seguridad y compatibilidad

`401`: JWT ausente, inválido, expirado o usuario inexistente. `404`: ruta/recurso inexistente o recurso ajeno. `400`: entrada inválida. `409`: correo duplicado, sensor ocupado o eliminación con dependencias. El detalle individual está disponible en `GET /api/v1/devices/{deviceId}/readings/{readingId}`, con los mismos campos del histórico. Una lectura inexistente o de otro dispositivo devuelve 404 `READING_NOT_FOUND`; una lectura ajena no es accesible. No se limita el detalle a 30 días.

Los errores usan Problem Details y no incluyen excepciones internas.

Las listas de perfiles, parcelas, dispositivos, reportes, pedidos, notificaciones e inventarios se filtran por dueño. Las referencias recibidas en los contratos antiguos también se comprueban. Los perfiles públicos de comunidad, sus comentarios públicos y el catálogo de productos siguen siendo legibles anónimamente; las escrituras exigen autenticación y propiedad cuando corresponde.

El registro ahora exige `confirmPassword`: la ausencia o una diferencia exacta con la contraseña devuelve 400. Android también deberá validar la confirmación antes de enviar. Se conserva el login con correo y contraseña solamente.

Se conservan las rutas y campos anteriores, incluido el `PUT` de perfil con `fundo_name`, `contact_phone`, `moisture_threshold` y `temp_threshold`. Se añaden campos opcionales para registros antiguos. La restricción de recursos ajenos es intencional. Crear un dispositivo por MAC también requiere un sensor provisionado y disponible. Su MAC registrada no puede sustituirse por otra identidad.

Los inventarios antiguos no tenían dueño: la migración los conserva con `owner_user_id = NULL` y quedan ocultos hasta que el equipo atribuya cada registro a un usuario verificado mediante mantenimiento administrativo. No se asignan arbitrariamente al primer usuario.

## Pruebas y cobertura

Las pruebas crean bases nuevas `terratech_tb1_<UUID>` en un **servidor MySQL de pruebas aislado**, con acceso para crear bases. No usar una conexión de producción. Los datos se conservan para inspección y pueden eliminarse después manualmente por ese prefijo en el servidor de pruebas.

```sh
export TB1_TEST_MYSQL='server=localhost;port=33308;user=root'
export DOTNET_ROOT='/ruta/al/dotnet10'
dotnet test TerraTech.IntegrationTests -c Debug --collect:'XPlat Code Coverage' --settings TerraTech.IntegrationTests/coverage.runsettings --results-directory TestResults
```

Cubren confirmación obligatoria/exacta, detalle de lectura protegido, límites temporales con reloj controlado, HTTP, credenciales, JWT, dos cuentas, asociaciones concurrentes, lecturas/unidades, claves foráneas, restricciones, migración válida y rechazo de datos huérfanos/duplicados; también ejecutan los comandos reales de demostración. Las migraciones y el código generado se excluyen de cobertura. [Matriz TB1](docs/TB1_MATRIX.md) separa alcance funcional y cobertura de líneas.

Microsoft.OpenApi se fija en 2.7.5, la versión corregida mínima de la línea 2.x para [GHSA-v5pm-xwqc-g5wc](https://github.com/microsoft/OpenAPI.NET/security/advisories/GHSA-v5pm-xwqc-g5wc).

## Límites de esta entrega

No incluye Android, verificación de correo, recuperación de contraseña, IA, recomendaciones, electroválvulas, clima, compras nuevas ni notificaciones push. Los reportes estadísticos anteriores conservan su significado y no reemplazan las mediciones. El backend está preparado para Cloud Run; no se ha desplegado ni creado una base remota. Ver [guía Cloud Run](docs/CLOUD_RUN.md), con construcción AMD64 en Debug, secretos, Job migrador y probes de salud.

## Organización de clases

Cada tipo tiene su propio archivo. Los recursos de perfil y lecturas están en `Interfaces/REST/Resources` de su contexto; los controladores contienen las acciones HTTP. `ApiFailure` y `ApiExceptionHandler` están separados, al igual que `Result` y `Result<T>`. Los fixtures de integración (`TestServer`, `ControlledClock`) también tienen archivos propios. La separación se validó con las 49 pruebas Debug contra MySQL aislado; [resultado](docs/modularity-validation.json).

## Cloud Run

La API está preparada para Cloud Run con `PORT`, HTTPS terminado por el proxy, probes públicos y migraciones automáticas optativas mediante `Database__MigrateOnStartup=true` (también admite Job separado). [Instrucciones](docs/CLOUD_RUN.md). `cloudbuild.yaml` construye/publica la imagen; el despliegue sigue siendo explícito. GitHub Actions ejecuta la suite Debug contra MySQL 8.4 aislado y construye AMD64 en cada cambio a main. La validación local pasó **59 pruebas**, más **15 pasos HTTP** dentro del contenedor Production; [evidencia](docs/cloud-run-validation.json). No se ha desplegado en GCP.

Inicialización automática en Cloud Run: **61 pruebas Debug aprobadas**, más creación de una base inexistente y esquema desde el contenedor Production AMD64 con `Database__MigrateOnStartup=true`; [evidencia](docs/startup-migration-validation.json). La conexión debe contener un nombre de base y permisos DDL. Los datos de demostración siguen siendo explícitos.

Cambios de experiencia Android: [sesión inmediata y contornos persistidos](docs/UX_SESSION_MAP.md).

Validación del refactor de sesión/mapas: **69 pruebas Debug aprobadas**, incluidas las pruebas anteriores de arranque y migración; [resultado](docs/ux-validation.json).


## PostgreSQL en Supabase y despliegue manual

Seleccionar `Database__Provider=PostgreSql` y configurar la conexión mediante
Secret Manager. Las migraciones PostgreSQL usan el esquema `terratech`.
Consultar [Supabase](docs/SUPABASE.md) para la conexión, migraciones y pruebas.
La construcción manual usa `cloudbuild.manual.yaml`. El workflow de GitHub
Actions fue retirado de las ramas principales.
