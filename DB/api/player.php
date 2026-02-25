<!-- Vollständig mit KI gemacht -->
<?php
require_once '../config/database.php';

$db = new Database();
$conn = $db->connect();

$input = json_decode(file_get_contents("php://input"), true);
$name = $input['playername'] ?? $_GET['playername'] ?? null;

if (!$name) jsonError("playername fehlt");

// Spieler suchen
$stmt = $conn->prepare("SELECT * FROM players WHERE playername = ?");
$stmt->execute([$name]);
$player = $stmt->fetch(PDO::FETCH_ASSOC);

if ($player) {
    jsonResponse([
        'success' => true,
        'exists' => true,
        'player' => $player
    ]);
}

// Neuen Spieler erstellen
$stmt = $conn->prepare("INSERT INTO players (playername) VALUES (?)");
$stmt->execute([$name]);
$id = $conn->lastInsertId();

jsonResponse([
    'success' => true,
    'exists' => false,
    'player' => [
        'id' => $id,
        'playername' => $name,
        'rounds_played' => 0,
        'wins' => 0,
        'losses' => 0,
        'playtime_seconds' => 0
    ]
]);
?>