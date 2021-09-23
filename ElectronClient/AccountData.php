<?php
$con=mysqli_connect("localhost","root","","kms_316");

if (mysqli_connect_errno())
  {
  echo "Failed to connect to MySQL: " . mysqli_connect_error();
  }

$username = htmlspecialchars(mysql_real_escape_string($_GET['username']));

$result = mysqli_query($con,"SELECT * FROM accounts WHERE name = '$username'");

while($row = mysqli_fetch_array($result))
  {
	echo $row['gm'];
	echo "<>";
	echo $row['vpoints'];
	echo "<>";
	echo $row['ACash'];
   }
?>
