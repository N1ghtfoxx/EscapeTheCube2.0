<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>EscapeTheCube - DB Test</title>
    <style>
        * { box-sizing: border-box; font-family: 'Segoe UI', sans-serif; }
        body { 
            max-width: 800px; 
            margin: 0 auto; 
            padding: 20px; 
            background: #1a1a2e;
            color: #eee;
        }
        h1 { color: #4ecca3; text-align: center; }
        .section {
            background: #16213e;
            padding: 20px;
            margin: 20px 0;
            border-radius: 10px;
            border-left: 4px solid #4ecca3;
        }
        h2 { color: #4ecca3; margin-top: 0; }
        input, button, select {
            padding: 10px;
            margin: 5px;
            border-radius: 5px;
            border: none;
        }
        input { background: #0f3460; color: white; width: 200px; }
        button { 
            background: #4ecca3; 
            color: #1a1a2e; 
            cursor: pointer;
            font-weight: bold;
        }
        button:hover { background: #3db892; }
        .output {
            background: #0f3460;
            padding: 15px;
            margin-top: 10px;
            border-radius: 5px;
            min-height: 100px;
            white-space: pre-wrap;
            font-family: monospace;
            font-size: 12px;
        }
        .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
    </style>
</head>
<body>
    <h1>🎮 EscapeTheCube - DB Test</h1>

    <div class="section">
        <h2>Stats Hinzufügen (Additiv)</h2>
        <div class="grid">
            <div>
                <label>Spielername:</label><br>
                <input type="text" id="playerName" value="Spieler1">
            </div>
            <div>
                <label>Runden:</label><br>
                <input type="number" id="rounds" value="5" min="0">
            </div>
            <div>
                <label>Ergebnis:</label><br>
                <select id="result">
                    <option value="win">Sieg (+1 Win)</option>
                    <option value="loss">Niederlage (+1 Loss)</option>
                    <option value="">Keine Änderung</option>
                </select>
            </div>
            <div>
                <label>Spielzeit (Sek):</label><br>
                <input type="number" id="playtime" value="300" min="0">
            </div>
        </div>
        <button onclick="addStats()" style="width: 100%; margin-top: 10px;">Zu Spieler addieren</button>
        <div id="output" class="output">Bereit...</div>
    </div>

    <script>
        const API_URL = window.location.href.replace('index.html', '') + 'api/';

        async function addStats() {
            const output = document.getElementById('output');
            const data = {
                playername: document.getElementById('playerName').value,
                rounds_played: parseInt(document.getElementById('rounds').value) || 0,
                result: document.getElementById('result').value,
                playtime_seconds: parseInt(document.getElementById('playtime').value) || 0
            };
            
            output.innerHTML = 'Sende...';
            
            try {
                const res = await fetch(API_URL + 'update.php', {
                    method: 'POST',
                    headers: {'Content-Type': 'application/json'},
                    body: JSON.stringify(data)
                });
                const result = await res.json();
                
                if (result.success) {
                    const p = result.player;
                    output.innerHTML = `✓ ${p.playername} aktualisiert!\n\n` +
                                     `Runden: ${p.rounds_played}\n` +
                                     `Wins: ${p.wins} | Losses: ${p.losses}\n` +
                                     `Spielzeit: ${Math.floor(p.playtime_seconds/60)}m ${p.playtime_seconds%60}s\n` +
                                     `Letztes Spiel: ${p.last_played}`;
                } else {
                    output.innerHTML = '✗ Fehler: ' + result.error;
                }
            } catch(e) {
                output.innerHTML = '✗ Netzwerkfehler: ' + e.message;
            }
        }
    </script>
</body>
</html>