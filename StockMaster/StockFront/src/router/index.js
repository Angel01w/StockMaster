import { createRouter, createWebHistory } from "vue-router";
import AppLayout from "../components/AppLayout.vue";
import Dashboard from "../components/Dashboard.vue";

const routes = [
    {
        path: "/",
        component: AppLayout,
        children: [
            { path: "", redirect: "/dashboard" },
            { path: "dashboard", component: Dashboard },
        ],
    },
];

export default createRouter({
    history: createWebHistory(),
    routes,
});
