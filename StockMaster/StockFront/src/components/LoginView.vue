<template>
	<div class="page">
		<div class="left">
			<div class="brand">
				<div class="logo">
					<img src="/Stock.ico" width="260" height="200" alt="StockMaster" />
				</div>
				<h1>StockMaster</h1>
				<p>Sistema Inteligente de Gestión de Inventarios</p>
			</div>
		</div>

		<div class="right">
			<div class="card">
				<h2>Iniciar Sesión</h2>

				<form @submit.prevent="onSubmit">
					<label class="lbl">Usuario</label>
					<div class="inputWrap">
						<span class="icon">👤</span>
						<input v-model.trim="form.username"
							   type="text"
							   placeholder="Ingresa tu usuario"
							   autocomplete="username"
							   :disabled="loading"
							   required />
					</div>

					<label class="lbl">Contraseña</label>
					<div class="inputWrap">
						<span class="icon">🔒</span>
						<input v-model="form.password"
							   :type="showPass ? 'text' : 'password'"
							   placeholder="Ingresa tu contraseña"
							   autocomplete="current-password"
							   :disabled="loading"
							   required />
						<button class="eyeBtn"
								type="button"
								@click="showPass = !showPass"
								:disabled="loading"
								aria-label="Mostrar u ocultar contraseña"
								title="Mostrar/Ocultar">
							{{ showPass ? "🙈" : "👁️" }}
						</button>
					</div>

					<div class="row">
						<a class="link" href="#" @click.prevent="onForgot">¿Olvidaste tu contraseña?</a>
					</div>

					<button class="btn" type="submit" :disabled="loading">
						{{ loading ? "Ingresando..." : "Ingresar" }}
					</button>

					<p v-if="error" class="error">{{ error }}</p>

					<div class="copy">© 2026 StockMaster - Todos los derechos reservados.</div>
				</form>
			</div>
		</div>
	</div>
</template>

<script setup>
	import { reactive, ref } from "vue";
	import { useRouter } from "vue-router";
	import { login } from "../router/auth.service";

	const router = useRouter();

	const form = reactive({
		username: "",
		password: "",
	});

	const loading = ref(false);
	const error = ref("");
	const showPass = ref(false);

	async function onSubmit() {
		error.value = "";
		loading.value = true;

		try {
			await login({
				username: form.username,
				password: form.password,
			});

			router.replace("/dashboard");
		} catch (e) {
			const msg =
				e?.response?.data?.message ||
				e?.response?.data?.msg ||
				e?.message ||
				"No se pudo iniciar sesión.";
			error.value = msg;
		} finally {
			loading.value = false;
		}
	}

	function onForgot() {
		alert("Funcionalidad de recuperación pendiente.");
	}
</script>

<style scoped>
	.page {
		min-height: 100vh;
		display: grid;
		grid-template-columns: 1.1fr 1fr;
		background: radial-gradient(circle at 20% 30%, rgba(255, 255, 255, 0.18), transparent 45%), radial-gradient(circle at 70% 60%, rgba(255, 255, 255, 0.14), transparent 55%), linear-gradient(135deg, #5b7cff 0%, #7c63ff 55%, #6fb6ff 100%);
	}

	.left {
		position: relative;
		padding: 64px;
		display: flex;
		align-items: center;
	}

	.brand {
		max-width: 520px;
		color: #fff;
	}

		.brand h1 {
			margin: 18px 0 8px;
			font-size: 52px;
			letter-spacing: -0.5px;
		}

		.brand p {
			margin: 0;
			font-size: 16px;
			opacity: 0.9;
		}

	.right {
		display: grid;
		place-items: center;
		padding: 32px;
	}

	.card {
		width: 440px;
		max-width: 92vw;
		background: rgba(255, 255, 255, 0.92);
		border: 1px solid rgba(255, 255, 255, 0.7);
		border-radius: 18px;
		padding: 28px 28px 18px;
		box-shadow: 0 18px 50px rgba(18, 23, 38, 0.25);
		backdrop-filter: blur(12px);
	}

		.card h2 {
			margin: 6px 0 18px;
			text-align: center;
			font-size: 22px;
			color: #1d2433;
		}

	.lbl {
		display: block;
		font-size: 13px;
		color: #2f3a52;
		margin: 12px 0 6px;
	}

	.inputWrap {
		position: relative;
		display: flex;
		align-items: center;
	}

	.icon {
		position: absolute;
		left: 12px;
		opacity: 0.7;
		font-size: 16px;
	}

	input {
		width: 100%;
		height: 44px;
		border-radius: 10px;
		border: 1px solid #e3e7f2;
		padding: 0 44px 0 40px;
		outline: none;
		background: #fff;
		font-size: 14px;
	}

		input:focus {
			border-color: #7b8dff;
			box-shadow: 0 0 0 4px rgba(123, 141, 255, 0.18);
		}

	.eyeBtn {
		position: absolute;
		right: 10px;
		border: none;
		background: transparent;
		cursor: pointer;
		opacity: 0.75;
		font-size: 16px;
		padding: 6px;
	}

	.row {
		display: flex;
		justify-content: flex-end;
		margin: 10px 0 16px;
	}

	.btn {
		width: 100%;
		height: 46px;
		border: none;
		border-radius: 10px;
		background: linear-gradient(90deg, #2f6bff, #2a55ff);
		color: #fff;
		font-weight: 700;
		cursor: pointer;
		box-shadow: 0 10px 22px rgba(47, 107, 255, 0.28);
	}

		.btn:disabled {
			opacity: 0.75;
			cursor: not-allowed;
		}

	.link {
		color: #2a55ff;
		font-size: 13px;
		text-decoration: none;
	}

	.copy {
		margin-top: 12px;
		text-align: center;
		font-size: 12px;
		color: #7b869d;
	}

	.error {
		margin-top: 10px;
		color: #b42318;
		font-size: 13px;
		text-align: center;
	}
</style>