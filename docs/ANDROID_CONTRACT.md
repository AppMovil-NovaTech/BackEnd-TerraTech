# Contrato Android del TB1

Base: `/api/v1`. JSON camelCase en los contratos nuevos. JWT en `Authorization: Bearer <token>`. ID enteros, fechas ISO 8601 UTC, superficie en m². Android convierte m² a hectáreas dividiendo entre 10 000.

| Paso | Endpoint | Resultado |
|---|---|---|
| Registro | `POST /authentication/sign-up` | 201 `{id,emailAddress,fullName,token:null,expiresAt:null,emailVerificationRequired:true}` |
| Login | `POST /authentication/sign-in` | 200 `{id,emailAddress,token,fullName,expiresAt}` |
| Cuenta | `GET /users/me` | Datos públicos de la cuenta propia |
| Perfil | `GET /profiles/me` | 200 perfil propio; 404 si aún no se completó |
| Completar/editar perfil | `PUT /profiles/me` | Upsert propio; no modifica correo |
| Parcelas | `GET /fields`, `POST /fields`, `GET/PUT/DELETE /fields/{id}` | Solo propias; `cropName` adicional opcional |
| Sensores | `GET /devices`, `GET /fields/{fieldId}/devices` | Solo propios |
| Asociar sensor | `POST /devices/register` | 201 dispositivo identificado |
| Última lectura | `GET /devices/{id}/readings/latest` | `{reading,isStale}`; 404 código `NO_READINGS` cuando vacío |
| Detalle de lectura | `GET /devices/{deviceId}/readings/{readingId}` | Un `ReadingResource` persistido del dispositivo propio, incluso anterior a 30 días |
| Histórico | `GET /devices/{id}/readings?days=7` o `30` | Rango UTC, referencia mínima y colección cronológica |

Registro: `{fullName,emailAddress,password,confirmPassword}`. Nombre mínimo dos caracteres, correo válido, contraseña mínima seis caracteres. `confirmPassword` es obligatorio y debe coincidir exactamente con `password`, incluyendo mayúsculas y espacios. Android valida la coincidencia antes de enviar y el backend vuelve a comprobarla. Ausencia: 400; diferencia: 400 `PASSWORD_CONFIRMATION_MISMATCH`. No se recortan contraseñas. Este requisito sustituye el contrato anterior que validaba la confirmación únicamente en Android. Correo duplicado: 409 `EMAIL_EXISTS`. Credenciales incorrectas: 401 `INVALID_CREDENTIALS`.

Perfil:

```json
{"fullName":"Ana Torres","fundoName":"Fundo Norte","contactPhone":"999888777","location":"Huaral, Lima","sizeM2":10000,"moistureThreshold":30,"tempThreshold":35}
```

`moistureThreshold` y `tempThreshold` son opcionales: al editar se conservan los existentes; un perfil nuevo usa 30 % y 35 °C. La respuesta añade `id`, `userId` y `emailAddress`. Los registros antiguos pueden tener `fullName`, `location` o `sizeM2` nulos. El perfil antiguo por ID conserva campos y admite su actualización snake_case.

Parcela:

```json
{"profileId":1,"name":"Parcela Norte","sizeM2":5000,"soilType":"Franco","latitude":-11.5,"longitude":-77.2,"cropName":"Papa"}
```

El `profileId` se comprueba contra el usuario autenticado. Suelo es un texto de máximo 50 caracteres; no existe un catálogo de IDs de suelo en este contrato. No enviar un `userId` inventado para cambiar el dueño.

Asociación:

```json
{"sensorCode":"TT-ZZZ001","fieldId":1,"name":"Sensor Norte"}
```

Respuesta: `{id,fieldId,macAddress,status,lastSync,sensorCode,name}`. La parcela debe pertenecer al usuario. Código inválido/desconocido: 400; sensor ocupado: 409 `SENSOR_OCCUPIED`. Las fechas/estados antiguos se conservan, pero no equivalen a batería medida. El código de dispositivos antiguos se provisiona automáticamente desde su ID, sin modificar su MAC ni asociación; su nombre puede ser nulo.

Última lectura:

```json
{"reading":{"id":1,"deviceId":1,"recordedAt":"2026-10-04T02:00:00Z","moisturePercent":42,"soilTemperatureC":23,"nitrogenPpm":35,"phosphorusPpm":18,"potassiumPpm":60,"source":"SIMULATED"},"isStale":false}
```

`isStale` es verdadero cuando han pasado más de 30 minutos desde la muestra. Histórico: `{deviceId,fromUtc,toUtc,minimumMoisturePercent,readings:[...]}`. Por defecto siete días; solo admite 7 o 30. Los extremos corresponden a la consulta, incluyen muestras del intervalo y nunca agregan reportes estadísticos. Sin muestras: `readings: []`. Unidades: humedad %, temperatura del suelo °C, N/P/K ppm. La etiqueta `SIMULATED` debe aparecer en la interfaz de demostración.

El detalle devuelve el mismo recurso que cada elemento de `readings` y que `reading` en la última medición. Una lectura inexistente o que pertenece a otro dispositivo devuelve 404 `READING_NOT_FOUND`; un dispositivo ajeno o inexistente devuelve 404 `DEVICE_NOT_FOUND`. Sin JWT devuelve 401. La consulta no modifica los datos.

## Caché Room

Guardar cuenta, perfil, parcelas, dispositivos y lecturas por ID, además de `recordedAt`, `source` y una fecha local de última sincronización. Asociar toda caché al usuario de la sesión y evitar mostrar datos de otra cuenta. Calcular la antigüedad también offline: el `isStale` guardado puede quedar desactualizado. Identificar la vista offline y la fecha de los datos. Las recomendaciones locales del móvil se calculan con reglas explícitas; no se presentan como confianza de IA ni batería medida. El pronóstico incluye su recomendación combinada documentada en Swagger. La pantalla de detalle offline usa la lectura guardada en Room por `(deviceId, id)`; no necesita conectarse a la ruta de detalle. Para datos locales registrar `downloadedAt` en Android, y recalcular la antigüedad al mostrar la pantalla. A los 30 minutos exactos el dato todavía no se considera antiguo; después sí.

Un JWT expirado no invalida los datos ya cacheados para lectura local, pero Android debe renovar la sesión antes de volver a consultar la API. No cachear contraseñas; proteger el token por separado.

## Errores

Problem Details: `type`, `title`, `status`, `traceId` cuando corresponda y `code` para casos distinguibles. Validación automática puede añadir `errors`. Resolver 401 como sesión inválida; 404 `PROFILE_NOT_FOUND` como perfil pendiente; 404 `NO_READINGS` como sensor sin datos; 409 como conflicto. Los recursos ajenos devuelven 404 para no revelar su existencia.

Sin verificación de correo ni recuperación de contraseña en TB1. Snapshot: `backend-openapi.snapshot.json`, exportado del backend compilado Debug. No confundir cobertura de líneas con el porcentaje funcional de la rúbrica.

## Actualización de sesión y mapas

Desde Sprint 2, las cuentas nuevas deben verificar su correo antes de obtener sesión. El login verificado emite un JWT de ocho horas; las cuentas históricas mantienen la exención acordada. `boundary` es opcional en las parcelas y contiene vértices `{latitude,longitude}`. El servidor calcula área y ubicación cuando se envía un contorno válido. Omitirlo en PUT lo preserva; `[]` lo elimina. Ver [UX_SESSION_MAP.md](UX_SESSION_MAP.md).

## Sprint 2: verificación y recuperación

El registro nuevo requiere verificación de correo y no inicia sesión automáticamente. `EMAIL_NOT_VERIFIED` (403) permite reenviar el enlace desde Android. Los endpoints `POST /authentication/email-verifications` y `POST /authentication/password-reset-requests` reciben `{emailAddress}` y responden 202 con un mensaje genérico. `EMAIL_UNAVAILABLE` (503) indica indisponibilidad real; 429 requiere esperar.

Los enlaces HTTPS abren las páginas `/account/verify-email` y `/account/reset-password`, que confirman mediante POST; los tokens no viajan en query strings. Verificación: 24 horas; recuperación: una hora. Son de un solo uso y se guardan como hash. Restablecer la contraseña invalida JWT anteriores. `/profiles/me` incorpora `registeredAt` nullable; no se inventan fechas faltantes.

El remitente temporal de pruebas Resend `onboarding@resend.dev` solo permite enviar al propietario de la cuenta. Cambiar a un remitente verificado para otros usuarios. Las claves quedan exclusivamente en configuración del backend.
