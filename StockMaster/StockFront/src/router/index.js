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
            { path: "usuarios", name: "usuarios", component: Usuarios, meta: { requiresAdmin: true } },
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

router.beforeEach((to) => {
    const token = localStorage.getItem("sm_token");
    const raw = localStorage.getItem("sm_user");
    const user = raw ? JSON.parse(raw) : null;

    const roleRaw = (user?.rol || user?.role || "").toString().trim().toLowerCase();
    const isAdmin = roleRaw === "admin" || roleRaw === "administrador" || roleRaw.includes("admin");

    if (to.meta.requiresAuth && !token) return { name: "login" };
    if (to.meta.guestOnly && token) return { name: "dashboard" };
    if (to.meta.requiresAdmin && !isAdmin) return { name: "dashboard" };

    return true;
});

export default router;