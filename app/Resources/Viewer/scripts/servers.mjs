import state from "./state.mjs";

const servers = (() => {
    let currentServerId = localStorage.getItem("selectedServerId") || "0";

    function getIcon(name) {
        return name.split(" ").map(word => {
            if (word.startsWith("[") && word.length > 1) return word[1];
            return word[0] || "";
        }).join("");
    }

    function update() {
        const channels = document.querySelectorAll("#channels .channel");
        const serversMap = new Map();

        // Check if there are any channels with server-id 0 (DM)
        const hasDMChannels = Array.from(channels).some(channel => {
            return channel.getAttribute("server-id") === "0";
        });

        if (hasDMChannels) {
            serversMap.set("0", {
                id: "0",
                name: "DM",
                icon: "DM",
            });
        }

        channels.forEach(channel => {
            const serverId = channel.getAttribute("server-id") || "0";
            const serverType = channel.getAttribute("server-type");
            const serverName = channel.getAttribute("server-name");

            if (serverType === "server" && !serversMap.has(serverId)) {
                serversMap.set(serverId, {
                    id: serverId,
                    name: serverName,
                    icon: getIcon(serverName),
                });
            }
        });

        const serversDiv = document.getElementById("servers");
        serversDiv.innerHTML = "";

        if (hasDMChannels) {
            const dmServer = serversMap.get("0");
            const dmElement = document.createElement("div");
            dmElement.className = `Server${dmServer.id === currentServerId ? " active" : ""}`;
            dmElement.id = "DM";
            dmElement.dataset.serverId = dmServer.id;
            dmElement.innerHTML = `
                <svg class="ServerImg" aria-hidden="true" role="img" xmlns="http://www.w3.org/2000/svg" width="30" height="30" fill="none" viewBox="0 0 24 24">
                    <path fill="currentColor" d="M19.73 4.87a18.2 18.2 0 0 0-4.6-1.44c-.21.4-.4.8-.58 1.21-1.69-.25-3.4-.25-5.1 0-.18-.41-.37-.82-.59-1.2-1.6.27-3.14.75-4.6 1.43A19.04 19.04 0 0 0 .96 17.7a18.43 18.43 0 0 0 5.63 2.87c.46-.62.86-1.28 1.2-1.98-.65-.25-1.29-.55-1.9-.92.17-.12.32-.24.47-.37 3.58 1.7 7.7 1.7 11.28 0l.46.37c-.6.36-1.25.67-1.9.92.35.7.75 1.35 1.2 1.98 2.03-.63 3.94-1.6 5.64-2.87.47-4.87-.78-9.09-3.3-12.83ZM8.3 15.12c-1.1 0-2-1.02-2-2.27 0-1.24.88-2.26 2-2.26s2.02 1.02 2 2.26c0 1.25-.89 2.27-2 2.27Zm7.4 0c-1.1 0-2-1.02-2-2.27 0-1.24.88-2.26 2-2.26s2.02 1.02 2 2.26c0 1.25-.88 2.27-2 2.27Z"></path>
                </svg>
            `;
            dmElement.addEventListener("click", () => selectServer(dmServer.id));
            serversDiv.appendChild(dmElement);
        }

        serversMap.forEach(server => {
            if (server.id === "0") return; // Skip DM since it's already added

            const serverElement = document.createElement("div");
            serverElement.className = `Server${server.id === currentServerId ? " active" : ""}`;
            serverElement.dataset.serverId = server.id;
            serverElement.innerHTML = `
                <div class="icon">${server.icon}</div>
                <div class="name" title="${server.name}">${server.name}</div>
            `;
            serverElement.addEventListener("click", () => selectServer(server.id));
            serversDiv.appendChild(serverElement);
        });

        if (!serversMap.has(currentServerId)) {
            currentServerId = "0";
            localStorage.setItem("selectedServerId", currentServerId);
        }

        updateChannelVisibility();
    }

    function selectServer(serverId) {
        // Remove active class from all servers
        document.querySelectorAll("#servers .Server").forEach(server => {
            server.classList.remove("active");
        });

        // Add active class to the selected server
        const selectedServer = document.querySelector(`#servers .Server[data-server-id="${serverId}"]`);
        if (selectedServer) {
            selectedServer.classList.add("active");
        }

        currentServerId = serverId;
        localStorage.setItem("selectedServerId", serverId);
        updateChannelVisibility();
        state.selectChannel(null);
    }

    function updateChannelVisibility() {
        document.querySelectorAll("#channels .channel").forEach(channel => {
            const channelServerId = channel.getAttribute("server-id") || "0";
            channel.classList.toggle("visible", channelServerId === currentServerId);
        });
    }

    return { update };
})();

export default servers;