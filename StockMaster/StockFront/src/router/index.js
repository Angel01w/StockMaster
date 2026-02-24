import { createRouter, createWebHistory } from "vue-router";

import AppLayout from "../components/AppLayout.vue";
import Login from "../components/LoginView.vue";

import Dashboard from "../components/Dashboard.vue";
import Productos from "../components/Productos.vue";
import Categorias from "../components/Categorias.vue";
import Proveedores from "../components/Proveedores.vue";
import Inventario from "../components/Inventario.vue";
import Reportes from "../components/Reportes.vue";
import Usuarios from "../components/Usuarios.vue";

const routes = [
    {
        path: "/login",
        name: "login",
        component: Login,
        meta: { guestOnly: true },
    },

    {
        path: "/",
        component: AppLayout,
        meta: { requiresAuth: true },
        children: [
            { path: "", redirect: "/dashboard" },
            { path: "dashboard", name: "dashboard", component: Dashboard },
            { path: "productos", name: "productos", component: Productos },
            { path: "categorias", name: "categorias", component: Categorias },
            { path: "proveedores", name: "proveedores", component: Proveedores },
            { path: "inventario", name: "inventario", component: Inventario },
            { path: "reportes", name: "reportes", component: Reportes },

            // 👇 Admin Y Auditor pueden entrar (pero Auditor será solo lectura en la UI)
            { path: "usuarios", name: "usuarios", component: Usuarios, meta: { requiresAdminOrAuditor: true } },
        ],
    },

    {
        path: "/:pathMatch(.*)*",
        redirect: () => {
            const token = localStorage.getItem("sm_token");
            return token ? "/dashboard" : "/login";
        },
    },
];

const router = createRouter({
    history: createWebHistory(),
    routes,
});

function safeParseUser() {
    try {
        const raw = localStorage.getItem("sm_user");
        return raw ? JSON.parse(raw) : null;
    } catch {
        localStorage.removeItem("sm_user");
        return null;
    }
}

function normalizeRole(user) {
    const raw = (user?.rol || user?.role || user?.roleLabel || "").toString().trim().toLowerCase();
    if (!raw) return "";
    if (raw.includes("admin")) return "admin";
    if (raw.includes("audit")) return "auditor";
    return "usuario";
}

router.beforeEach((to) => {
    const token = localStorage.getItem("sm_token");
    const user = safeParseUser();
    const role = normalizeRole(user);

    const isAdmin = role === "admin";
    const isAuditor = role === "auditor";

    if (to.meta.requiresAuth && !token) return { name: "login" };
    if (to.meta.guestOnly && token) return { name: "dashboard" };

    if (to.meta.requiresAdminOrAuditor && !(isAdmin || isAuditor)) return { name: "dashboard" };

    return true;
});

export default router;