import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Banking AI Bot",
  description: "A banking POC with AI-powered insights, spending analysis, and guided savings."
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <div className="app-shell">{children}</div>
      </body>
    </html>
  );
}
