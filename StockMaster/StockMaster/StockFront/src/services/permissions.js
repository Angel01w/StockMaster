export const ROLES = {
    ADMIN: "admin",
    AUDITOR: "auditor",
    USUARIO: "usuario",
};

export function normalizeRole(raw) {
    const r = (raw || "").toString().trim().toLowerCase();
    if (r.includes("admin")) return ROLES.ADMIN;
    if (r.includes("audit")) return ROLES.AUDITOR;
    return ROLES.USUARIO;
}

export const PERMISSIONS = {
    [ROLES.ADMIN]: {
        canView: true,
        canEditAll: true,
        canManageUsers: true,
        canEditInventario: true,
        canViewUsuarios: true,
        canEditUsuarios: true,
        canViewCatalogos: true,
        canEditCatalogos: true,
        canViewReportes: true,
        canEditReportes: true,
        canDownloadReportes: true,
        canReadAll: true,
    },
    [ROLES.AUDITOR]: {
        canView: true,
        canEditAll: false,
        canManageUsers: false,
        canEditInventario: false,
        canViewUsuarios: true,
        canEditUsuarios: false,
        canViewCatalogos: true,
        canEditCatalogos: false,
        canViewReportes: true,
        canEditReportes: false,
        canDownloadReportes: false,
        canReadAll: true,
    },
    [ROLES.USUARIO]: {
        canView: true,
        canEditAll: false,
        canManageUsers: false,
        canEditInventario: true,
        canViewUsuarios: false,
        canEditUsuarios: false,
        canViewCatalogos: true,
        canEditCatalogos: true,
        canViewReportes: true,
        canEditReportes: false,
        canDownloadReportes: false,
        canReadAll: false,
    },
};

export function getPerms(role) {
    return PERMISSIONS[normalizeRole(role)] || PERMISSIONS[ROLES.USUARIO];
}

function pickUsername(u) {
    const v =
        u?.username ??
        u?.Username ??
        u?.userName ??
        u?.UserName ??
        u?.login ??
        u?.Login ??
        "";
    return (v || "").toString().trim().toLowerCase();
}

const DOWNLOAD_WHITELIST = new Set(["a.jimenezc", "am.sierra"]);

export function getPermsSafe(userOrRole) {
    try {
        const rawRole =
            typeof userOrRole === "string"
                ? userOrRole
                : (userOrRole?.rol || userOrRole?.role || userOrRole?.roleLabel || "");

        const base = getPerms(rawRole);

        if (typeof userOrRole === "object" && userOrRole) {
            const username = pickUsername(userOrRole);
            if (DOWNLOAD_WHITELIST.has(username)) {
                return { ...base, canDownloadReportes: true };
            }
        }

        return base;
    } catch {
        return PERMISSIONS[ROLES.USUARIO];
    }
}

export function roleLabel(role) {
    const r = normalizeRole(role);
    if (r === ROLES.ADMIN) return "Admin";
    if (r === ROLES.AUDITOR) return "Auditor";
    return "Usuario";
}