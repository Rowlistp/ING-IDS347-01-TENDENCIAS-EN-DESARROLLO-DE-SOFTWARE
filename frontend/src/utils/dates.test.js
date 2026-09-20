import assert from 'node:assert/strict'
import { execFileSync } from 'node:child_process'
import test from 'node:test'

for (const zone of ['America/Santo_Domingo', 'America/Los_Angeles', 'UTC', 'Asia/Tokyo']) {
  test(`Una fecha de cierre conserva el día en ${zone}`, () => {
    const source = `import { parseCivilDate, formatDate, formatDateTime } from ${JSON.stringify(new URL('./dates.js', import.meta.url).href)};
      const day = parseCivilDate('2026-09-19');
      console.log(JSON.stringify([day.getFullYear(), day.getMonth() + 1, day.getDate(), formatDate('2026-09-19'), formatDateTime('2026-09-19')]));`
    const values = JSON.parse(execFileSync(process.execPath, ['--input-type=module', '-e', source], { env: { ...process.env, TZ: zone }, encoding: 'utf8' }))
    assert.deepEqual(values.slice(0, 3), [2026, 9, 19])
    assert.equal(values[3], '19/9/2026')
    assert.equal(values[4], '19/9/2026')
  })
}
