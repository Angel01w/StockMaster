import axios from "axios";

const API_BASE = "https://localhost:7198";

export async function login(payload) {
  try {
    const loginValue =
      payload?.Login ??
      payload?.Username ??
      payload?.username ??
      payload?.Email ??
      payload?.email ??
      payload?.login ??
      payload?.user ??
      "";

    const passValue = payload?.Password ?? payload?.password ?? "";

    if (!loginValue || !passValue) {
      throw new Error(
        `Faltan credenciales. login="${loginValue}" password=${passValue ? "***" : "(vacío)"}`
      );
    }

    const body = { Login: loginValue, Password: passValue };

    const res = await axios.post(`${API_BASE}/api/Auth/login`, body, {
      headers: { "Content-Type": "application/json" },
    });

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
  } catch (err) {
    console.error("LOGIN ERROR:", {
      status: err?.response?.status,
      data: err?.response?.data,
      msg: err?.message,
    });
    throw err;
  }
}

export function logout() {
  localStorage.removeItem("sm_token");
  localStorage.removeItem("sm_user");
}