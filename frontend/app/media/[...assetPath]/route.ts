const CONFIGURED_API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";
const INTERNAL_BACKEND_URL = process.env.BACKEND_INTERNAL_URL ?? CONFIGURED_API_BASE_URL;

function buildBackendAssetUrl(assetPath: string[]) {
  const backendUrl = new URL(INTERNAL_BACKEND_URL);
  backendUrl.pathname = `/${assetPath.join("/")}`;

  return backendUrl.toString();
}

function isSafeUploadPath(assetPath: string[]) {
  return (
    assetPath.length >= 2 &&
    assetPath[0] === "uploads" &&
    assetPath.every(
      (segment) =>
        segment.length > 0 &&
        segment !== "." &&
        segment !== ".." &&
        /^[a-zA-Z0-9._-]+$/.test(segment),
    )
  );
}

export async function GET(
  _request: Request,
  context: { params: Promise<{ assetPath: string[] }> },
) {
  const { assetPath } = await context.params;

  if (!assetPath?.length || !isSafeUploadPath(assetPath)) {
    return new Response("Not found", { status: 404 });
  }

  const backendUrl = buildBackendAssetUrl(assetPath);
  const response = await fetch(backendUrl, {
    cache: "no-store",
  });

  if (!response.ok) {
    return new Response("Not found", { status: response.status });
  }

  const headers = new Headers();
  const contentType = response.headers.get("content-type");
  const contentLength = response.headers.get("content-length");
  const cacheControl = response.headers.get("cache-control");

  if (contentType) {
    headers.set("content-type", contentType);
  }

  if (contentLength) {
    headers.set("content-length", contentLength);
  }

  headers.set("cache-control", cacheControl ?? "public, max-age=300");

  return new Response(response.body, {
    status: 200,
    headers,
  });
}
