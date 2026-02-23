import axios from "axios";

const API_BASE = "https://localhost:7198";

export async function login(payload) {
    const res = await axios.post(`${API_BASE}/api/Auth/login`, payload);
    const data = res.data;

    const token = data.token || data.accessToken || data.jwt || "";
    if (!token) throw new Error("No se recibió token del servidor.");

    const user = {
        idUsuario: data.idUsuario ?? data.id ?? data.userId ?? null,
        nombreCompleto: data.nombreCompleto ?? data.fullName ?? data.nombre ?? "",
        correo: data.correo ?? data.email ?? "",
        rol: data.rol ?? data.role ?? "",
        idProveedor: data.idProveedor ?? data.proveedorId ?? null,
    };

    localStorage.setItem("sm_token", token);
    localStorage.setItem("sm_user", JSON.stringify(user));

    return data;
}

export function logout() {
    localStorage.removeItem("sm_token");
    localStorage.removeItem("sm_user");
}