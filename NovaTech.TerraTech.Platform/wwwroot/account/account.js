"use strict";
const form = document.querySelector("form");
const statusText = document.getElementById("status");
const button = form.querySelector("button");
const token = new URLSearchParams(location.hash.slice(1)).get("token");
history.replaceState(null, "", location.pathname);
if (!token || token.length !== 43) {
    statusText.textContent = "El enlace no es válido. Solicita uno nuevo desde TerraTech.";
    button.disabled = true;
}
form.addEventListener("submit", async event => {
    event.preventDefault();
    const fields = Object.fromEntries(new FormData(form));
    if (fields.password !== fields.confirmPassword) {
        statusText.textContent = "Las contraseñas no coinciden.";
        return;
    }
    button.disabled = true;
    statusText.textContent = "Procesando…";
    try {
        const response = await fetch("/api/v1/authentication/" + form.dataset.endpoint, {
            method: "POST", headers: {"Content-Type": "application/json"},
            body: JSON.stringify({...fields, token})
        });
        if (response.ok) {
            statusText.textContent = "Listo. Ya puedes volver a TerraTech e iniciar sesión.";
            form.reset();
            return;
        }
        statusText.textContent = response.status === 429
            ? "Demasiados intentos. Espera un minuto antes de volver a intentar."
            : "No se pudo completar. El enlace puede haber vencido o ya fue utilizado. Solicita uno nuevo desde TerraTech.";
    } catch {
        statusText.textContent = "No se pudo conectar. Comprueba tu conexión e intenta nuevamente.";
    }
    button.disabled = false;
});
