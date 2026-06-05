<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

interface Payment {
  id: number
  orderId: string
  amount: number
  currency: string
  status: string
  createdAt: string
}

const payments = ref<Payment[]>([])
const connectionStatus = ref<'Conectando...' | 'Conectado' | 'Desconectado'>('Conectando...')

const paymentServiceBaseUrls = [
  import.meta.env.VITE_PAYMENT_SERVICE_URL,
  'http://localhost:5001',
  'http://localhost:5009',
  'https://localhost:7069'
].filter((url): url is string => Boolean(url))

let connection: signalR.HubConnection

onMounted(async () => {
  for (const baseUrl of paymentServiceBaseUrls) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/paymentHub`, {
        withCredentials: false
      })
      .withAutomaticReconnect()
      .build()

    connection.on('PaymentCreated', (payment: Payment) => {
      payments.value.unshift(payment)
    })

    try {
      await connection.start()
      connectionStatus.value = 'Conectado'
      return
    } catch (error) {
      await connection.stop()
      console.error(`Error conectando al Hub en ${baseUrl}:`, error)
    }
  }

  connectionStatus.value = 'Desconectado'
})

onUnmounted(async () => {
  if (connection) {
    await connection.stop()
  }
})
</script>

<template>
  <div class="dashboard">
    <h1>Tablero de Pagos en Tiempo Real</h1>
    <p class="status" :class="connectionStatus === 'Conectado' ? 'online' : 'offline'">
      Estado: {{ connectionStatus }}
    </p>

    <table v-if="payments.length > 0">
      <thead>
        <tr>
          <th>ID</th>
          <th>Order ID</th>
          <th>Monto</th>
          <th>Moneda</th>
          <th>Status</th>
          <th>Fecha</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="payment in payments" :key="payment.id">
          <td>{{ payment.id }}</td>
          <td>{{ payment.orderId }}</td>
          <td>{{ payment.amount }}</td>
          <td>{{ payment.currency }}</td>
          <td>{{ payment.status }}</td>
          <td>{{ new Date(payment.createdAt).toLocaleString() }}</td>
        </tr>
      </tbody>
    </table>

    <p v-else>Esperando pagos...</p>
  </div>
</template>

<style scoped>
.dashboard { max-width: 900px; margin: 40px auto; font-family: sans-serif; }
.status { font-weight: bold; }
.online { color: green; }
.offline { color: red; }
table { width: 100%; border-collapse: collapse; margin-top: 20px; }
th, td { border: 1px solid #ccc; padding: 10px; text-align: left; }
th { background: #f4f4f4; }
tr:hover { background: #f9f9f9; }
</style>