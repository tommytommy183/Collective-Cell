// wwwroot/game.js
const healthValue = document.getElementById('healthValue');
const mutationType = document.getElementById('mutationType');
const logElement = document.getElementById('log');
const canvas = document.getElementById('cellCanvas');
const ctx = canvas.getContext('2d');
const pressedKeys = {};

let connection = null;
let currentCellState = null;

// --- 1. SignalR 連線設定 ---
async function startSignalRConnection() {
    // 建立 SignalR 連線到 /cellHub
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/cellHub") // 記得與 Program.cs 中的 mapHub 一致
        .withAutomaticReconnect()
        .build();

    // 監聽伺服器廣播的狀態更新
    connection.on("ReceiveCellStateUpdate", (state) => {
        currentCellState = state;
        updateUI(state);
        drawCell(state);
    });

    // 監聽伺服器回覆的輸入確認
    connection.on("InputReceived", (type, amount) => {
        appendLog(`[You]: Contributed ${type} (${amount})`);
    });

    try {
        await connection.start();
        appendLog("SignalR 連線成功！可以開始貢獻了。");
    } catch (err) {
        appendLog(`SignalR 連線失敗: ${err.toString()}`);
        console.error(err);
    }
}

// --- 2. 遊戲邏輯與 UI 更新 ---
function updateUI(state) {
    healthValue.textContent = state.health.toFixed(2);
    mutationType.textContent = state.currentMutationType;
}

function drawCell(state) {
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // 計算細胞中心位置
    const centerX = state.x; 
    const centerY = state.y;
    
    // 根據大小繪製細胞
    const radius = state.size; 
    
    // 繪製資源點
    state.resources.forEach(res => {
        ctx.beginPath();
        ctx.arc(res.x, res.y, 5, 0, 2 * Math.PI);
        ctx.fillStyle = 'green';
        ctx.fill();
        ctx.closePath();
    });

    // 繪製細胞本體
    ctx.beginPath();
    ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);

    // 顏色與狀態掛鉤
    let color = 'rgba(0, 150, 255, 0.8)'; // 預設藍色
    if (state.currentMutationType === 'Giant') {
        color = 'rgba(255, 100, 0, 0.8)'; // 突變為橘色
    } else if (state.currentMutationType === 'Struggling') {
        color = 'rgba(150, 150, 150, 0.8)'; // 掙扎為灰色
    }

    ctx.fillStyle = color;
    ctx.fill();
    ctx.strokeStyle = 'black';
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.closePath();

    // 繪製 Health 標籤
    ctx.fillStyle = 'black';
    ctx.font = '14px Arial';
    ctx.textAlign = 'center';
    ctx.fillText(`Health: ${state.health.toFixed(0)}`, centerX, centerY + radius + 20);
}

function contributeMovement(direction) {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("ContributeMovement", direction)
            .catch(err => console.error(`Movement failed: ${err.toString()}`));
    } else {
        appendLog("連線未建立，無法移動。");
    }
}

document.getElementById('upBtn').addEventListener('click', () => contributeMovement('Up'));
document.getElementById('downBtn').addEventListener('click', () => contributeMovement('Down'));
document.getElementById('leftBtn').addEventListener('click', () => contributeMovement('Left'));
document.getElementById('rightBtn').addEventListener('click', () => contributeMovement('Right'));

function appendLog(message) {
    const p = document.createElement('p');
    p.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
    logElement.prepend(p);
    // 限制 log 數量
    if (logElement.children.length > 10) {
        logElement.removeChild(logElement.lastChild);
    }
}

// --- 3. 互動事件綁定 ---
document.getElementById('absorbBtn').addEventListener('click', () => {
    // 呼叫伺服器的 ContributeEnergy 方法
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        // 玩家點擊一次貢獻 10 點能量
        connection.invoke("ContributeEnergy", "Absorb", 10)
            .catch(err => console.error(err.toString()));
    } else {
        appendLog("連線未建立或已中斷。");
    }
});

document.getElementById('emitBtn').addEventListener('click', () => {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
        // 玩家點擊一次排放 20 點廢物 (即扣除 Health * 0.5 = 10 點)
        connection.invoke("ContributeEnergy", "Emit", 20)
            .catch(err => console.error(err.toString()));
    } else {
        appendLog("連線未建立或已中斷。");
    }
});


document.addEventListener('keydown', (event) => {
    // 檢查連線狀態
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        return;
    }

    let directionToSend = null;
    const key = event.key.toLowerCase();

    // 防止重複發送同一個方向（例如按住 'w' 不放）
    if (pressedKeys[key]) {
        return;
    }

    // 映射 WASD 和方向鍵
    switch (key) {
        case 'w':
        case 'arrowup':
            directionToSend = 'Up';
            break;
        case 's':
        case 'arrowdown':
            directionToSend = 'Down';
            break;
        case 'a':
        case 'arrowleft':
            directionToSend = 'Left';
            break;
        case 'd':
        case 'arrowright':
            directionToSend = 'Right';
            break;
        default:
            return; // 忽略其他按鍵
    }

    if (directionToSend) {
        // 標記該鍵為已按下
        pressedKeys[key] = true;

        // 呼叫後端 Hub 方法
        contributeMovement(directionToSend);
    }
});

// 釋放鍵盤時，標記按鍵為未按下
document.addEventListener('keyup', (event) => {
    const key = event.key.toLowerCase();
    pressedKeys[key] = false;
});

// 啟動連線
startSignalRConnection();