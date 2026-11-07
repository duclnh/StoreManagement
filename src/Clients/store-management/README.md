This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Frontend structure

- `app/` contains route groups: `(public)/login`, `(private)/dashboard`, and `(private)/settings`, plus the root redirect, layout, global styles, and identity API proxy.
- `services/api-client.ts` contains shared HTTP and error handling; the identity BFF remains under `app/api/identity/` to keep auth tokens in HTTP-only cookies.
- `features/identity-admin/` groups identity administration by responsibility:
  - `api/*.api.ts` contains endpoint functions grouped by identity domain.
  - `interfaces/` contains the domain and component contracts.
  - `components/` contains the login, account security, workspace navigation, and administration UI.
  - `locales/` contains the `next-intl` message catalogs.
- `features/employees/` is an empty scaffold for employee UI, hooks, API, schemas, types, stores, constants, and utilities. Add files when employee requirements and API contracts are defined.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
