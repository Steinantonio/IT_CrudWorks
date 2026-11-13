'use strict';
// QA helper: print the state of the seeded SQLite database.
//
// Why it exists: the API has no "delete user" endpoint (ADR-0004 defines
// register / login / me only), so the QA end-to-end script cannot restore the
// `Users` table through the public contract. QA needs a way to *observe* the
// table, both to verify the seeded baseline and to verify that a run of
// `docs/qa/e2e-api-check.js` leaves the database where it found it.
//
// Usage:
//   node docs/qa/tools/db-state.js            # users + task count
//   node docs/qa/tools/db-state.js --emails   # also list every user email
//
// The path defaults to the Development database of the API
// (`project/src/CrudWorks.Api/crudworks.db`) and can be overridden with the
// `CRUDWORKS_DB` environment variable.

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..', '..', '..');
const DB_PATH =
  process.env.CRUDWORKS_DB || path.join(ROOT, 'project', 'src', 'CrudWorks.Api', 'crudworks.db');

function main() {
  if (!fs.existsSync(DB_PATH)) {
    console.error('database file not found: ' + DB_PATH);
    process.exitCode = 2;
    return;
  }
  const { DatabaseSync } = require('node:sqlite');
  const db = new DatabaseSync(DB_PATH, { readOnly: true });
  try {
    const users = db.prepare('SELECT Id, Email, DisplayName FROM Users ORDER BY Email').all();
    const taskCount = db.prepare('SELECT COUNT(*) AS c FROM Tasks').get().c;
    const demoTasks = db
      .prepare(
        'SELECT COUNT(*) AS c FROM Tasks WHERE UserId = (SELECT Id FROM Users WHERE Email = ?) ',
      )
      .get('demo@crudworks.local').c;

    console.log('db: ' + path.relative(ROOT, DB_PATH));
    console.log('users: ' + users.length);
    if (process.argv.includes('--emails')) {
      for (const u of users) console.log('  - ' + u.Email + '  (' + u.DisplayName + ')');
    } else {
      for (const u of users) console.log('  - ' + u.Email);
    }
    console.log('tasks: ' + taskCount);
    console.log('tasks owned by demo@crudworks.local: ' + demoTasks);
    console.log(
      'BASELINE | ' +
        (users.length === 1 && users[0].Email === 'demo@crudworks.local' && taskCount === 5
          ? 'seeded baseline reached (1 demo user, 5 tasks)'
          : 'NOT the seeded baseline (expected 1 demo user and 5 tasks)'),
    );
  } finally {
    db.close();
  }
}

main();
