 <?php
## Database Information
$database = array(
        "host" => "localhost",
        "user" => "root",
        "pass" => "",
        "dbse" => "kms_316"
        );

## Connect To Database
mysql_connect($database['host'], $database['user'], $database['pass']) or die("0-Database server is offline.");
mysql_select_db($database['dbse']) or die("0-Database isn't available.");

## All the other stuff...
if (!empty($_GET['username']) && !empty($_GET['password'])) {
    $username = mysql_real_escape_string($_GET['username']);
    $password = mysql_real_escape_string($_GET['password']);

    $AQuery = sprintf("
    SELECT COUNT('id')
    FROM accounts
    WHERE name = '%s' AND password = '%s'",
    $username, $password);
    $aresult = mysql_query($AQuery);
    $atotal = mysql_result($aresult, 0);
    $reply = ($atotal > 0) ? true : false;

    // return $reply;
    if ($reply) {
        echo 'pplzdlnewclient';
    } else {
        echo '0-Wrong username or password.';
    }
}
?>
