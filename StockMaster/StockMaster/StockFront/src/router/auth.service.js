import axios from "axios";
import { normalizeRole, getPerms, roleLabel } from "../services/permissions";

const API_BASE = "https://localhost:7198";

const TOKEN_KEY = "sm_token";
const USER_KEY = "sm_user";

function pickToken() {
    return localStorage.getItem(TOKEN_KEY) || localStorage.getItem("token") || "";
}

function saveSession(token, user) {
    if (token) localStorage.setItem(TOKEN_KEY, token);
    if (user !== undefined) localStorage.setItem(USER_KEY, JSON.stringify(user));
}

function safeJson(v) {
    if (!v) return {};
    if (typeof v === "string") {
        try {
            return JSON.parse(v);
        } catch (e) {
            return {};
        }
    }
    return v;
}

function firstNonEmpty() {
    for (let i = 0; i < arguments.length; i++) {
        const v = arguments[i];
        if (v !== null && v !== undefined && String(v).trim() !== "") return v;
    }
    return "";
}

function get(obj, key) {
    if (!obj) return undefined;
    return obj[key];
}

function extractToken(data) {
    if (!data) return "";
    return firstNonEmpty(
        data.token,
        data.Token,
        data.accessToken,
        data.access_token,
        data.jwt,
        data.Jwt,
        data.bearer,
        data.Bearer,
        data.data && data.data.token
    );
}

function extractUserObj(data) {
    if (!data) return null;
    return data.user || data.User || data.usuario || data.Usuario || null;
}

function extractRoleRaw(data, userObj) {
    return firstNonEmpty(
        data && (data.rol || data.role || data.Rol || data.Role),
        userObj && (userObj.rol || userObj.role || userObj.Rol || userObj.Role)
    );
}

function buildUser(data, body) {
    const userObj = extractUserObj(data) || {};
    const rolRaw = extractRoleRaw(data, userObj);
    const role = normalizeRole(rolRaw);

    const areaId = firstNonEmpty(
        get(data, "areaId"),
        get(data, "AreaId"),
        get(data, "idArea"),
        get(data, "IdArea"),
        get(userObj, "areaId"),
        get(userObj, "AreaId"),
        get(userObj, "idArea"),
        get(userObj, "IdArea")
    );
    const areaIdVal = areaId === "" ? null : areaId;

    const idUsuario = firstNonEmpty(
        get(data, "idUsuario"),
        get(data, "IdUsuario"),
        get(data, "id"),
        get(data, "userId"),
        get(userObj, "idUsuario"),
        get(userObj, "IdUsuario"),
        get(userObj, "id"),
        get(userObj, "userId")
    );
    const idUsuarioVal = idUsuario === "" ? null : idUsuario;

    const nombreCompleto = firstNonEmpty(
        get(data, "nombreCompleto"),
        get(data, "NombreCompleto"),
        get(data, "fullName"),
        get(data, "nombre"),
        get(data, "Nombre"),
        get(userObj, "nombreCompleto"),
        get(userObj, "NombreCompleto"),
        get(userObj, "fullName"),
        get(userObj, "nombre"),
        get(userObj, "Nombre")
    );

    const correo = firstNonEmpty(
        get(data, "correo"),
        get(data, "email"),
        get(data, "Email"),
        get(userObj, "correo"),
        get(userObj, "email"),
        get(userObj, "Email")
    );

    const username = firstNonEmpty(
        get(data, "username"),
        get(data, "Username"),
        get(userObj, "username"),
        get(userObj, "Username"),
        body && body.Login
    );

    return {
        idUsuario: idUsuarioVal,
        nombreCompleto: nombreCompleto || "",
        correo: correo || "",
        username: username || "",
        rol: rolRaw,
        role: role,
        roleLabel: roleLabel(role),
        perms: getPerms(role),
        areaId: areaIdVal,
    };
}

function authHeaders(extra) {
    const t = pickToken();
    const h = { ...(extra || {}) };
    if (t) h.Authorization = `Bearer ${t}`;
    return h;
}

export async function login(payload) {
    try {
        const loginValue = firstNonEmpty(
            payload && payload.Login,
            payload && payload.Username,
            payload && payload.username,
            payload && payload.Email,
            payload && payload.email,
            payload && payload.login,
            payload && payload.user
        );

        const passValue = firstNonEmpty(payload && payload.Password, payload && payload.password);

        if (!loginValue || !passValue) {
            throw new Error(
                'Faltan credenciales. login="' +
                String(loginValue || "") +
                '" password=' +
                (passValue ? "***" : "(vacío)")
            );
        }

        const body = { Login: String(loginValue).trim(), Password: String(passValue) };

        const res = await axios.post(API_BASE + "/api/Auth/login", body, {
            headers: { "Content-Type": "application/json" },
        });

        const data = safeJson(res && res.data);

        const token = extractToken(data);
        if (!token) throw new Error("No se recibió token del servidor.");

        const user = buildUser(data, body);
        saveSession(token, user);

        const out = {};
        if (data && typeof data === "object") {
            for (const k in data) out[k] = data[k];
        }
        out.token = token;
        out.user = user;

        return out;
    } catch (err) {
        const e = err;
        const resp = e && e.response ? e.response : null;

        console.error("LOGIN ERROR:", {
            status: resp ? resp.status : undefined,
            data: resp ? resp.data : undefined,
            msg: e && e.message ? e.message : "Unknown error",
        });

        throw err;
    }
}

export function logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem("token");
    localStorage.removeItem("user");
}

export function getToken() {
    return pickToken();
}

export function isAuthenticated() {
    return !!getToken();
}

export function getUser() {
    try {
        const raw = localStorage.getItem(USER_KEY);
        if (!raw) return null;

        const u = JSON.parse(raw);
        const role = normalizeRole(firstNonEmpty(u && (u.role || u.rol || u.Role || u.Rol)));

        const out = {};
        if (u && typeof u === "object") {
            for (const k in u) out[k] = u[k];
        }
        out.role = role;
        out.roleLabel = roleLabel(role);
        out.perms = (u && u.perms) ? u.perms : getPerms(role);

        return out;
    } catch (e) {
        localStorage.removeItem(USER_KEY);
        return null;
    }
}

export function getRole() {
    const u = getUser();
    return normalizeRole(u ? u.role : "");
}

export function getPermsSafe() {
    const u = getUser();
    return (u && u.perms) ? u.perms : getPerms(getRole());
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

export async function apiGet(path, config) {
    const url = path.startsWith("http") ? path : API_BASE + path;
    return axios.get(url, { ...(config || {}), headers: authHeaders((config && config.headers) || {}) });
}

export async function apiPost(path, data, config) {
    const url = path.startsWith("http") ? path : API_BASE + path;
    return axios.post(url, data, { ...(config || {}), headers: authHeaders((config && config.headers) || {}) });
}

export async function apiPut(path, data, config) {
    const url = path.startsWith("http") ? path : API_BASE + path;
    return axios.put(url, data, { ...(config || {}), headers: authHeaders((config && config.headers) || {}) });
}

export async function apiDelete(path, config) {
    const url = path.startsWith("http") ? path : API_BASE + path;
    return axios.delete(url, { ...(config || {}), headers: authHeaders((config && config.headers) || {}) });
}