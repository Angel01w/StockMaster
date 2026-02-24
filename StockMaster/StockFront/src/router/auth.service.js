import axios from "axios";
import { normalizeRole, getPerms, roleLabel } from "../services/permissions";

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

        const rolRaw =
            data.rol ??
            data.role ??
            data.Rol ??
            data.Role ??
            data?.user?.rol ??
            data?.user?.role ??
            "";

        const role = normalizeRole(rolRaw);

        const user = {
            idUsuario:
                data.idUsuario ??
                data.id ??
                data.userId ??
                data?.user?.idUsuario ??
                data?.user?.id ??
                null,
            nombreCompleto:
                data.nombreCompleto ??
                data.fullName ??
                data.nombre ??
                data?.user?.nombreCompleto ??
                "",
            correo: data.correo ?? data.email ?? data?.user?.correo ?? "",
            rol: rolRaw,
            role,
            roleLabel: roleLabel(role),
            perms: getPerms(role),
            idProveedor: data.idProveedor ?? data.proveedorId ?? data?.user?.idProveedor ?? null,
        };

        localStorage.setItem("sm_token", token);
        localStorage.setItem("sm_user", JSON.stringify(user));

        return { ...data, token, user };
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

export function getToken() {
    return localStorage.getItem("sm_token");
}

export function getUser() {
    try {
        const raw = localStorage.getItem("sm_user");
        if (!raw) return null;

        const u = JSON.parse(raw);
        const role = normalizeRole(u.role || u.rol || u.roleLabel);

        return {
            ...u,
            role,
            roleLabel: roleLabel(role),
            perms: u.perms || getPerms(role),
        };
    } catch {
        localStorage.removeItem("sm_user");
        return null;
    }
}

export function getRole() {
    return normalizeRole(getUser()?.role);
}

export function getPermsSafe() {
    return getUser()?.perms || getPerms(getRole());
}

export function isAdmin() {
    return getRole() === "admin";
}

export function isAuditor() {
    return getRole() === "auditor";
}

export function isUsuario() {
    return getRole() === "usuario";
}