import { getToken, logout } from "../router/auth.service";

const API_BASE = "https://localhost:7198";

function isAbsoluteUrl(v) {
    return typeof v === "string" && (v.startsWith("http://") || v.startsWith("https://"));
}

function buildUrl(path) {
    if (!path) return API_BASE;
    if (isAbsoluteUrl(path)) return path;
    if (typeof path !== "string") return `${API_BASE}`;
    return path.startsWith("/") ? `${API_BASE}${path}` : `${API_BASE}/${path}`;
}

async function parseError(res) {
    const ct = res.headers.get("content-type") || "";
    try {
        if (ct.includes("application/json")) {
            const data = await res.json();
            const msg = data?.message || data?.msg || data?.error || data?.title || JSON.stringify(data);
            return msg || `${res.status}`;
        }
        const txt = await res.text();
        return txt || `${res.status}`;
    } catch {
        return `${res.status}`;
    }
}

export async function apiFetch(path, options = {}) {
    const token = getToken();

    const headers = new Headers(options.headers || {});
    const hasBody = options.body !== undefined && options.body !== null;

    if (!headers.has("Accept")) headers.set("Accept", "application/json");

    const isFormData = typeof FormData !== "undefined" && options.body instanceof FormData;
    if (!headers.has("Content-Type") && hasBody && !isFormData) headers.set("Content-Type", "application/json");

    if (token) headers.set("Authorization", `Bearer ${token}`);

    const res = await fetch(buildUrl(path), {
        ...options,
        headers
    });

    if (res.status === 401) {
        logout();
        throw new Error("401");
    }

    if (!res.ok) {
        const msg = await parseError(res);
        throw new Error(msg);
    }

    if (res.status === 204) return null;

    const ct = res.headers.get("content-type") || "";
    if (ct.includes("application/json")) return await res.json();
    return await res.text();
}