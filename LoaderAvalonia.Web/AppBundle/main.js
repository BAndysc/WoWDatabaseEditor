import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

dotnetRuntime.setModuleImports("main.js", {
    window: {
        history: {
            replaceState: (title, href) => globalThis.window.history.replaceState(null, title, href)
        }
    },
    localStorage: {
        getValue: (key) => globalThis.localStorage.getItem(key),
        setValue: (key, value) => globalThis.localStorage.setItem(key, value)
    }
});

const config = dotnetRuntime.getConfig();

let href = window.location.href;
if (href.indexOf("?") !== -1)
    href = href.slice(0, href.indexOf("?"))
if (href.indexOf("index.html") !== -1)
    href = href.slice(0, href.indexOf("index.html"))

await dotnetRuntime.runMain(config.mainAssemblyName, ["--search", window.location.search, "--href", href]);