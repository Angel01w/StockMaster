<template>
	<div class="shell">
		<aside class="side">
			<div class="brand">
				<img src="../assets/Stock.png" width="50" height="50" />
				<span class="btext">StockMaster</span>
			</div>

			<nav class="menu">

				<RouterLink to="/dashboard" class="mi" active-class="active">
					<i class="dot"></i> Dashboard
				</RouterLink>
				<RouterLink to="/productos" class="mi" active-class="active">
					<i class="dot"></i> Productos
				</RouterLink>
				<RouterLink to="/categorias" class="mi" active-class="active">
					<i class="dot"></i> Categorías
				</RouterLink>
				<RouterLink to="/proveedores" class="mi" active-class="active">
					<i class="dot"></i> Proveedores
				</RouterLink>
				<RouterLink to="/inventario" class="mi" active-class="active">
					<i class="dot"></i> Inventario
				</RouterLink>
				<RouterLink to="/reportes" class="mi" active-class="active">
					<i class="dot"></i> Reportes
				</RouterLink>
				<RouterLink to="/usuarios" class="mi" active-class="active">
					<i class="dot"></i> Usuarios
				</RouterLink>
				<RouterLink to="/configuracion" class="mi" active-class="active">
					<i class="dot"></i> Configuración
				</RouterLink>
			</nav>

			<div class="logout">
				<button class="logoutBtn">Log out</button>
			</div>
		</aside>

		<main class="main">
			<header class="top">
				<div class="crumb">
					<div class="ic"></div>
					<div class="ctext">
						<div class="c1">Dashboard</div>
					</div>
				</div>

				<div class="topRight">
					<button class="iconBtn" title="Notificaciones">🔔</button>
					<button class="iconBtn" title="Buscar">🔍</button>
					<div class="user">
						<img class="avatar" src="https://i.pravatar.cc/38?img=3" alt="Admin" />
						<span class="uname">Admin</span>
					</div>
				</div>
			</header>

			<div class="content">
				<router-view />
			</div>
		</main>
	</div>
</template>

<script setup>
import { RouterLink } from "vue-router";
</script>

<style scoped>
	.shell {
		min-height: 100vh;
		background: radial-gradient(900px 500px at 80% 20%, rgba(168,85,247,.22), transparent 60%), radial-gradient(800px 460px at 15% 30%, rgba(59,130,246,.18), transparent 55%), #eef3ff;
		display: flex;
	}

	.side {
		width: 250px;
		background: linear-gradient(180deg, #0e2a5a, #163b7d);
		color: #fff;
		display: flex;
		flex-direction: column;
		padding: 14px;
		border-right: 1px solid rgba(255,255,255,.12);
	}

	.brand {
		display: flex;
		align-items: center;
		gap: 10px;
		padding: 8px 10px 14px;
		font-weight: 900;
	}

	.logo {
		width: 30px;
		height: 30px;
		object-fit: contain;
	}

	.btext {
		letter-spacing: .2px;
	}

	.menu {
		display: flex;
		flex-direction: column;
		gap: 8px;
		padding: 6px 6px;
	}

	.mi {
		color: rgba(255,255,255,.86);
		text-decoration: none;
		padding: 10px 12px;
		border-radius: 10px;
		display: flex;
		align-items: center;
		gap: 10px;
		font-weight: 800;
		font-size: 13px;
	}

		.mi .dot {
			width: 8px;
			height: 8px;
			border-radius: 999px;
			background: rgba(255,255,255,.35);
		}

		.mi:hover {
			background: rgba(255,255,255,.10);
		}

		.mi.active {
			background: rgba(59,130,246,.40);
			border: 1px solid rgba(255,255,255,.14);
		}

			.mi.active .dot {
				background: #fff;
			}

	.logout {
		margin-top: auto;
		padding: 10px 6px 6px;
	}

	.logoutBtn {
		width: 100%;
		padding: 10px 12px;
		border-radius: 10px;
		border: 1px solid rgba(255,255,255,.14);
		background: rgba(255,255,255,.08);
		color: #fff;
		font-weight: 900;
	}

	.main {
		flex: 1;
		display: flex;
		flex-direction: column;
	}

	.top {
		height: 64px;
		background: rgba(255,255,255,.92);
		border-bottom: 1px solid rgba(15,23,42,.08);
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0 16px;
		box-shadow: 0 10px 22px rgba(10, 20, 70, .08);
	}

	.crumb {
		display: flex;
		align-items: center;
		gap: 10px;
	}

	.ic {
		width: 36px;
		height: 36px;
		border-radius: 12px;
		background: rgba(59,130,246,.12);
		border: 1px solid rgba(59,130,246,.18);
	}

	.c1 {
		font-weight: 900;
		color: #0f172a;
	}

	.topRight {
		display: flex;
		align-items: center;
		gap: 10px;
	}

	.iconBtn {
		width: 36px;
		height: 36px;
		border-radius: 12px;
		border: 1px solid rgba(15,23,42,.08);
		background: rgba(15,23,42,.03);
		cursor: pointer;
	}

	.user {
		display: flex;
		align-items: center;
		gap: 10px;
		padding-left: 8px;
	}

	.avatar {
		width: 34px;
		height: 34px;
		border-radius: 999px;
		border: 2px solid rgba(59,130,246,.25);
	}

	.uname {
		font-weight: 900;
		color: #0f172a;
	}

	.content {
		padding: 14px;
	}
</style>
