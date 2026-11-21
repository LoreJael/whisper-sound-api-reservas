document.addEventListener("DOMContentLoaded", () => {
    let token = "";
    let connection = null;

    // Referencias a elementos del HTML
    const pantallaLogin = document.getElementById("pantallaLogin");
    const pantallaPanel = document.getElementById("pantallaPanel");
    const formLogin = document.getElementById("formLogin");
    const usuarioInput = document.getElementById("usuarioInput");
    const passwordInput = document.getElementById("passwordInput");
    const errorLogin = document.getElementById("errorLogin");
    const estadoConexion = document.getElementById("estadoConexion");
    const tablaReservas = document.getElementById("tablaReservas");
    const listaActividad = document.getElementById("listaActividad");
    const estadoResumen = document.getElementById("estadoResumen");
    const resumenTotal = document.getElementById("resumenTotal");
    const resumenPendientes = document.getElementById("resumenPendientes");
    const resumenConfirmadas = document.getElementById("resumenConfirmadas");
    const resumenCanceladas = document.getElementById("resumenCanceladas");

    // 1. Iniciar sesión con la API REST (AuthController)
    formLogin.addEventListener("submit", async (e) => {
        e.preventDefault();
        errorLogin.textContent = "";

        const respuesta = await fetch("/api/Auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                username: usuarioInput.value,
                password: passwordInput.value
            })
        });

        if (!respuesta.ok) {
            errorLogin.textContent = "Usuario o contraseña incorrectos.";
            return;
        }

        const datos = await respuesta.json();
        token = datos.token;

        pantallaLogin.classList.add("oculto");
        pantallaPanel.classList.remove("oculto");

        await cargarReservas();
        await conectarHub();
        conectarResumen();
    });

    // 2. Traer las reservas desde la API REST (ReservasController)
    async function cargarReservas() {
        const respuesta = await fetch("/api/Reservas", {
            headers: { "Authorization": "Bearer " + token }
        });
        const reservas = await respuesta.json();

        tablaReservas.innerHTML = "";

        if (reservas.length === 0) {
            tablaReservas.innerHTML = `<tr><td colspan="6" class="vacio">Aún no hay reservas. Crea una desde Swagger y aparecerá aquí.</td></tr>`;
            return;
        }

        reservas.forEach(r => {
            const fecha = new Date(r.fecha);
            const fila = document.createElement("tr");

            fila.innerHTML = `
                <td class="col-id">${r.id}</td>
                <td>${r.cliente ? r.cliente.nombre : "Paciente " + r.clienteId}</td>
                <td>${r.nombreProfesional}</td>
                <td>
                    <span class="fecha-dia">${fecha.toLocaleDateString("es-CL", { weekday: "short", day: "numeric", month: "short" })}</span>
                    <span class="fecha-hora">${fecha.toLocaleTimeString("es-CL", { hour: "2-digit", minute: "2-digit", hour12: false })} hrs</span>
                </td>
                <td><span id="estado-${r.id}" class="estado estado-${r.estado.toLowerCase()}">${r.estado}</span></td>
                <td class="col-accion">
                    <select id="select-${r.id}" aria-label="Nuevo estado de la reserva ${r.id}">
                        <option>Pendiente</option>
                        <option>Confirmada</option>
                        <option>Cancelada</option>
                    </select>
                    <button class="btn-guardar" data-id="${r.id}">Guardar</button>
                </td>`;

            tablaReservas.appendChild(fila);
            document.getElementById(`select-${r.id}`).value = r.estado;
        });
    }

    // 3. Conectarse al Hub de SignalR enviando el token JWT
    async function conectarHub() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/reservashub", { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .build();

        // Mensaje que envía el Hub cuando alguien cambia un estado
        connection.on("EstadoActualizado", (reservaId, nuevoEstado, usuario) => {
            const etiqueta = document.getElementById(`estado-${reservaId}`);
            const select = document.getElementById(`select-${reservaId}`);

            if (etiqueta) {
                etiqueta.textContent = nuevoEstado;
                etiqueta.className = `estado estado-${nuevoEstado.toLowerCase()}`;
            }
            if (select) {
                select.value = nuevoEstado;
            }

            agregarActividad(`${usuario} cambió la reserva N° ${reservaId} a ${nuevoEstado}.`);
        });

        // Mensaje que envía ReservasController cuando se crea una reserva
        connection.on("ReservaCreada", async (reservaId) => {
            agregarActividad(`Se registró la reserva N° ${reservaId}.`);
            await cargarReservas();
        });

        // Mensaje de error que el Hub envía solo a quien hizo la llamada
        connection.on("ErrorReserva", (mensaje) => {
            agregarActividad(mensaje, true);
        });

        try {
            await connection.start();
            estadoConexion.textContent = "Conectado";
            estadoConexion.classList.add("conectado");
        } catch (err) {
            agregarActividad("No se pudo conectar al servidor en tiempo real.", true);
            console.error(err);
        }
    }

    // 5. Escuchar el resumen de reservas con Server-Sent Events (EventosController)
    function conectarResumen() {
        // EventSource no permite enviar headers, por eso el token va en la URL
        const fuente = new EventSource("/api/Eventos/resumen?access_token=" + token);

        // Se ejecuta cada vez que el servidor envía un evento con "event: resumen"
        fuente.addEventListener("resumen", (e) => {
            const resumen = JSON.parse(e.data);

            resumenTotal.textContent = resumen.total;
            resumenPendientes.textContent = resumen.pendientes;
            resumenConfirmadas.textContent = resumen.confirmadas;
            resumenCanceladas.textContent = resumen.canceladas;
        });

        // La conexión SSE se abrió (o se volvió a abrir después de un corte)
        fuente.onopen = () => {
            estadoResumen.textContent = "Resumen en vivo";
            estadoResumen.classList.add("conectado");
        };

        // La conexión se cortó: EventSource intenta reconectarse solo cada 5 segundos
        fuente.onerror = () => {
            estadoResumen.classList.remove("conectado");

            if (fuente.readyState === EventSource.CLOSED) {
                // El servidor rechazó la conexión (por ejemplo, token vencido) y ya no reintenta
                estadoResumen.textContent = "Resumen detenido";
            } else {
                estadoResumen.textContent = "Reconectando resumen...";
            }
        };
    }

    // 4. Botón "Guardar" de cada fila: llama al método CambiarEstado del Hub
    tablaReservas.addEventListener("click", async (e) => {
        if (!e.target.classList.contains("btn-guardar")) return;

        const reservaId = parseInt(e.target.dataset.id);
        const nuevoEstado = document.getElementById(`select-${reservaId}`).value;

        await connection.invoke("CambiarEstado", reservaId, nuevoEstado);
    });

    // Agrega una línea al registro de actividad (la más nueva queda arriba)
    function agregarActividad(texto, esError = false) {
        const li = document.createElement("li");
        if (esError) li.classList.add("error");

        const hora = document.createElement("span");
        hora.className = "actividad-hora";
        hora.textContent = new Date().toLocaleTimeString("es-CL", { hour12: false });

        li.append(hora, texto);
        listaActividad.prepend(li);
    }
});
