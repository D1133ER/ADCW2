# Integrating React with ASP.NET Core via SpaProxy

This guide walks you through migrating your existing standalone Vite + React application (`/blog-guardian-main`) directly inside your ASP.NET Core application (`/ADCW2/Weblog Application.csproj`) to create a unified system.

By following these steps, running your backend in Visual Studio will automatically boot up your frontend `npm run dev` script, tying them together perfectly.

## Step 1: Move the Frontend Project
Currently, your frontend and backend sit in separate folders. The standard convention is to have the frontend nested inside the backend.

1. Close any running servers (`Ctrl+C`).
2. Move the entire `blog-guardian-main` folder into your `ADCW2` directory.
3. Rename the moved `blog-guardian-main` folder to `ClientApp`.

Your new directory structure should look like this:
```
/ADCW2
  ├── Weblog Application.csproj
  ├── Program.cs
  ├── /Controllers
  └── /ClientApp                 <-- formerly blog-guardian-main
        ├── package.json
        ├── vite.config.ts
        └── /src
```

## Step 2: Install SpaProxy in ASP.NET
Open your terminal in the `ADCW2` folder and run this command. This installs the official Microsoft tool that auto-starts your Vite server and proxies requests.
```bash
dotnet add package Microsoft.AspNetCore.SpaProxy
```

## Step 3: Update `Weblog Application.csproj`
Open `Weblog Application.csproj`. You need to replace the `PublishRunWebpack` chunk and add proxy configurations.

Make your `<PropertyGroup>` look like this by adding the `Spa` fields:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <!-- Tells .NET where to look for package.json -->
  <SpaRoot>ClientApp\</SpaRoot>
  <SpaProxyLaunchCommand>npm run dev</SpaProxyLaunchCommand>
  <SpaProxyServerUrl>http://localhost:5173</SpaProxyServerUrl>
</PropertyGroup>
```

Add these items right above your `PackageReference` blocks. This ensures git and MSBuild don't accidentally try to track/compile your `node_modules`.
```xml
<ItemGroup>
  <Content Remove="$(SpaRoot)**" />
  <None Remove="$(SpaRoot)**" />
  <None Include="$(SpaRoot)**" Exclude="$(SpaRoot)node_modules\**" />
</ItemGroup>
```

Replace the old `<Target Name="PublishRunWebpack">` block at the bottom with:
```xml
<Target Name="PublishRunWebpack" AfterTargets="ComputeFilesToPublish">
  <Exec WorkingDirectory="$(SpaRoot)" Command="npm install" />
  <Exec WorkingDirectory="$(SpaRoot)" Command="npm run build" />
  <ItemGroup>
    <DistFiles Include="$(SpaRoot)dist\**" />
    <ResolvedFileToPublish Include="@(DistFiles->'%(FullPath)')" Exclude="@(ResolvedFileToPublish)">
      <RelativePath>wwwroot\%(RecursiveDir)%(FileName)%(Extension)</RelativePath>
      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
      <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
    </ResolvedFileToPublish>
  </ItemGroup>
</Target>
```

## Step 4: Configure Vite Dev Proxy
Now that the backend knows how to start Vite, we need Vite to know how to send API requests (like `/api/blogs`) to ASP.NET. 

Based on your `ADCW2/Properties/launchSettings.json`, your ASP.NET API runs on `https://localhost:7078` and `http://localhost:5155`.

Open `/ADCW2/ClientApp/vite.config.ts` and add the `server` block to setup the proxy:
```typescript
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react-swc';
import path from "path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  // Add this new server block:
  server: {
    port: 5173,
    proxy: {
      '/api': {
        // Pointing to your https ASP.NET launch port
        target: 'https://localhost:7078', 
        changeOrigin: true,
        // Disable secure check for local development certificates
        secure: false, 
      }
    }
  }
});
```

## Step 5: Test and Run
1. Open your terminal at the ASP.NET level (`/ADCW2/`)
2. Run `dotnet run` (or press the green Run/Play button if you use Visual Studio).
3. The ASP.NET server will automatically detect `ClientApp` and launch Vite in the background. Navigate to `http://localhost:5173` in your browser. All API calls made by the React app will now be successfully tunneled back to ASP.NET!
