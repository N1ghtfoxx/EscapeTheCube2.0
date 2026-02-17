<?php
require_once '../config/database.php';

$db = new Database();
$conn = $db->connect();

$input = json_decode(file_get_contents("php://input"), true);

if (!isset($input['playername'])) jsonError("playername fehlt");

$name = $input['playername'];
$rounds = $input['rounds_played'] ?? 0;
$result = $input['result'] ?? null;
$playtime = $input['playtime_seconds'] ?? 0;

// Spieler existiert? Falls nicht, ERSTELLEN
$stmt = $conn->prepare("SELECT id FROM players WHERE playername = ?");
$stmt->execute([$name]);
if (!$stmt->fetch()) {
    $stmt = $conn->prepare("INSERT INTO players (playername) VALUES (?)");
    $stmt->execute([$name]);
}

// Update-Query bauen
$updates = ["rounds_played = rounds_played + ?"];
$params = [$rounds];

if ($result === 'win') {
    $updates[] = "wins = wins + 1";
} elseif ($result === 'loss') {
    $updates[] = "losses = losses + 1";
}

if ($playtime > 0) {
    $updates[] = "playtime_seconds = playtime_seconds + ?";
    $params[] = $playtime;
}

$params[] = $name;

$sql = "UPDATE players SET " . implode(", ", $updates) . " WHERE playername = ?";
$stmt = $conn->prepare($sql);
$stmt->execute($params);

// Aktuelle Daten zurückgeben
$stmt = $conn->prepare("SELECT * FROM players WHERE playername = ?");
$stmt->execute([$name]);
$player = $stmt->fetch(PDO::FETCH_ASSOC);

jsonResponse([
    'success' => true,
    'updated' => true,
    'player' => $player
]);
?>