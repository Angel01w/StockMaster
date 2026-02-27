const API_BASE = "https://localhost:7198";

function pickToken() {
    return localStorage.getItem("sm_token") || localStorage.getItem("token") || "";
}

export async function apiFetch(url, options) {
    const finalUrl = /^https?:\/\//i.test(url) ? url : (API_BASE + url);
    const token = pickToken();

    const opt = options || {};
    const hdrs = opt.headers ? { ...opt.headers } : {};

    if (!hdrs["Content-Type"] && !(opt.body instanceof FormData)) {
        hdrs["Content-Type"] = "application/json";
    }

    if (token) {
        hdrs["Authorization"] = "Bearer " + token;
    }

    const res = await fetch(finalUrl, { ...opt, headers: hdrs });

    if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || (res.status + " " + res.statusText));
    }

    const ct = res.headers.get("content-type") || "";
    if (ct.includes("application/json")) return await res.json();

    const t = await res.text();
    try { return t ? JSON.parse(t) : null; } catch { return t; }
}