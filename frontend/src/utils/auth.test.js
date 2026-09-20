import test from 'node:test'
import assert from 'node:assert/strict'
import { getUser, isAuthenticated, saveSession, clearSession, subscribeSession } from '../services/auth.js'

globalThis.window = new EventTarget()
const data = new Map()
globalThis.localStorage = { getItem: (k) => data.get(k) ?? null, setItem: (k, v) => data.set(k, String(v)), removeItem: (k) => data.delete(k) }

test('corrupt or malformed cached user cannot create an authenticated session', () => {
  for (const value of ['{bad', 'null', '{}', '{"roles":1}', '{"usuarioId":1,"nombreUsuario":"qa","roles":[null]}']) {
    data.set('user', value); data.set('token', 'test')
    assert.equal(getUser(), null)
    assert.equal(isAuthenticated(), false)
  }
})

test('session writes and cross-tab events notify subscribers; logout clears all credentials', () => {
  let changes = 0
  const unsubscribe = subscribeSession(() => changes++)
  saveSession({ accessToken: 'test', refreshToken: 'refresh', usuarioId: 1, nombreUsuario: 'qa', roles: ['Consulta'] })
  assert.equal(isAuthenticated(), true)
  window.dispatchEvent(new Event('storage'))
  clearSession()
  assert.equal(changes, 3)
  assert.equal(isAuthenticated(), false)
  assert.equal(data.size, 0)
  unsubscribe()
})
