import { createRouter, createWebHistory } from "vue-router";
import AppLayout from "../components/AppLayout.vue";

import Dashboard from "../components/Dashboard.vue";
import Productos from "../components/Productos.vue";
import Categorias from "../components/Categorias.vue";
import Proveedores from "../components/Proveedores.vue";
import Inventario from "../components/Inventario.vue";
import Reportes from "../components/Reportes.vue";
import Usuarios from "../components/Usuarios.vue";

const routes = [
    {
        path: "/",
        component: AppLayout,
        children: [
            { path: "", redirect: "/dashboard" },
            { path: "dashboard", name: "dashboard", component: Dashboard },
            { path: "productos", name: "productos", component: Productos },
            { path: "categorias", name: "categorias", component: Categorias },
            { path: "proveedores", name: "proveedores", component: Proveedores },
            { path: "inventario", name: "inventario", component: Inventario },
            { path: "reportes", name: "reportes", component: Reportes },
            { path: "usuarios", name: "usuarios", component: Usuarios },
        ],
    },
];

export default createRouter({
    history: createWebHistory(),
    routes,
});
