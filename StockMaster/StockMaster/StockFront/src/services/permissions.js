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
    },
};

export function getPerms(role) {
    return PERMISSIONS[normalizeRole(role)] || PERMISSIONS[ROLES.USUARIO];
}

export function getPermsSafe(userOrRole) {
    try {
        const raw =
            typeof userOrRole === "string"
                ? userOrRole
                : (userOrRole?.rol || userOrRole?.role || userOrRole?.roleLabel || "");
        return getPerms(raw);
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