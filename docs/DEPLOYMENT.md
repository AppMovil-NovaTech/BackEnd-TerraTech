# Migraciones y despliegue

El backend admite PostgreSQL en Supabase mediante Npgsql y migraciones separadas. Ver [Supabase](SUPABASE.md) para el despliegue manual actual. La guía [Cloud Run](CLOUD_RUN.md) conserva las instrucciones del proveedor MySQL anterior.

## Base existente

Respaldar la base y revisar las filas señaladas por el preflight antes de aplicar cambios. El arranque comprueba: perfiles sin usuario, más de un perfil por usuario, parcelas sin perfil, dispositivos sin parcela, MAC duplicadas al normalizar mayúsculas/separadores, reportes sin dispositivo y referencias de pedidos, notificaciones, comunidad y comentarios. Ante un problema se detiene y no borra filas.

Aplicar con el mismo camino de diagnóstico del servidor:

```sh
dotnet run --project NovaTech.TerraTech.Platform -c Debug --no-launch-profile -- --migrate-only
```

En un artefacto ya preparado: `dotnet NovaTech.TerraTech.Platform.dll --migrate-only`. Usar variables de entorno válidas y un usuario MySQL autorizado. No usar `EnsureDeleted`, `EnsureCreated` ni recrear bases. Evitar ejecutar `dotnet ef database update` directamente sobre datos antiguos, pues ese camino omite el preflight del arranque. Las migraciones se generan con `dotnet ef migrations add <Nombre> --configuration Debug`.

`Tb1Journey` es incremental. Añade columnas opcionales, catálogo, lecturas, índices únicos y claves foráneas con `RESTRICT`. Provisiona cada dispositivo antiguo con código `TT-` + ID en base 36 rellenado a seis caracteres. Ejemplo: ID 123 → `TT-00003F`. La transformación es determinista y conserva la MAC y parcela. `is_demo=false` evita presentar dispositivos antiguos como simulados sin autorización. No se generan mediciones automáticamente. Los inventarios antiguos quedan intactos y sin dueño asignado hasta revisión administrativa; no son públicos.

MySQL puede confirmar DDL por operación: realizar el respaldo y resolver preflight antes de migrar, y ejecutar una sola instancia migradora durante un despliegue. Las claves foráneas también detienen referencias inválidas introducidas concurrentemente. No ejecutar una migración Down para revertir lecturas nuevas sin un respaldo: su reversión elimina las columnas/tablas añadidas.

## Configuración remota a revisar

- SDK/runtime .NET 10; MySQL compatible con el proveedor EF Core usado.
- `ASPNETCORE_ENVIRONMENT=Production`.
- `ConnectionStrings__DefaultConnection`: cadena completa desde el gestor de secretos, con usuario y permisos adecuados.
- `TokenSettings__Secret`: clave aleatoria de al menos 32 bytes UTF-8, guardada en secretos; usar la misma en todas las instancias.
- `ASPNETCORE_URLS=http://+:8080` dentro del contenedor, con HTTPS terminado y configurado por el proveedor.
- Swagger seguirá público. Registro/login anónimos; resto según autorización documentada.
- Respaldar, ejecutar preflight/migración, iniciar API, ejecutar recorrido de verificación con cuenta temporal y revisar errores sanitizados.

Dockerfile preparado para publicar **Debug**, escuchar `PORT` (8080 por defecto) y ejecutar sin usuario root. En Production, `Database__MigrateOnStartup=true` permite crear la base si no existe y migrar al arrancar; con `false`, usar `--migrate-only` antes del servicio. La creación requiere un nombre de base en la conexión y permisos MySQL suficientes. Un bloqueo MySQL por base serializa creación, preflight y migraciones entre arranques concurrentes. Construcción a revisar: `docker build -t terratech-tb1 .`; pasar secretos al arrancar, nunca en capas de imagen ni archivos versionados. La validación local y sus límites se registran en [validación Cloud Run](cloud-run-validation.json); no se publicó en GCP.

Los comandos `--demo-catalog` y `--demo-readings` solo están disponibles en Development y fallan en Production antes de migrar. Para una demostración remota con datos simulados habrá que acordar expresamente el entorno y la provisión; no habilitar siembras automáticas al desplegar.
