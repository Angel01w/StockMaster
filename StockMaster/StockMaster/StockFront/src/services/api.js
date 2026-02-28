const API_BASE = "https://localhost:7198";

function readJwtExp(token) {
	try {
		const p = token.split(".")[1];
		if (!p) return 0;
		const b64 = p.replace(/-/g, "+").replace(/_/g, "/");
		const pad = b64.length % 4 ? "=".repeat(4 - (b64.length % 4)) : "";
		const json = atob(b64 + pad);
		const obj = JSON.parse(json);
		return Number(obj?.exp || 0);
	} catch {
		return 0;
	}
}

function pickToken() {
	const t1 = localStorage.getItem("token") || "";
	const t2 = localStorage.getItem("sm_token") || "";
	if (t1 && !t2) return t1;
	if (!t1 && t2) return t2;
	if (!t1 && !t2) return "";
	return readJwtExp(t1) >= readJwtExp(t2) ? t1 : t2;
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
		const st = `${res.status ?? 0} ${res.statusText || "Error"}`.trim();
		throw new Error(text?.trim() ? text : st);
	}

	const ct = res.headers.get("content-type") || "";
	if (ct.includes("application/json")) return await res.json();

	const t = await res.text();
	try { return t ? JSON.parse(t) : null; } catch { return t; }
}