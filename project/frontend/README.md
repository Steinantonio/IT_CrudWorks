# CrudWorks frontend

## Generated API types

`src/api/generated/openapi.d.ts` is generated from the API's Development OpenAPI
document. It is versioned with the repository and must be committed together
with the API change; commit approval follows the project workflow. Never edit it
by hand.

The generator is isolated under `tools/openapi-typescript/` with its own lockfile
and TypeScript 5.x, because `openapi-typescript` 7.13.0 requires the TypeScript 5
compiler API while this app uses TypeScript 7.

Regenerate after an API contract/metadata change:

```powershell
# once, or after the tool lockfile changes
npm --prefix tools/openapi-typescript ci

# start the API in Development (from project/)
dotnet run --project src/CrudWorks.Api --launch-profile http

# from this folder, with the API on http://localhost:5080
npm run generate:api-types
```

Review and commit the regenerated declaration together with the API change. The
runtime client remains `src/lib/api.ts` (`fetch`); no API client is generated.

---

# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some Oxlint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the Oxlint configuration

If you are developing a production application, we recommend enabling type-aware lint rules by installing `oxlint-tsgolint` and editing `.oxlintrc.json`:

```json
{
  "$schema": "./node_modules/oxlint/configuration_schema.json",
  "plugins": ["react", "typescript", "oxc"],
  "options": {
    "typeAware": true
  },
  "rules": {
    "react/rules-of-hooks": "error",
    "react/only-export-components": ["warn", { "allowConstantExport": true }]
  }
}
```

See the [Oxlint rules documentation](https://oxc.rs/docs/guide/usage/linter/rules) for the full list of rules and categories.
