import { expect, test } from '@playwright/test';

test('login page displays the sign-in form', async ({ page }) => {
  await page.goto('/en/login');

  await expect(page.getByRole('heading')).toBeVisible();
  await expect(page.getByRole('button', { name: /sign in|log in/i })).toBeVisible();
});
