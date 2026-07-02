$env:JAVA_HOME = "E:\software\jvms\store\1.8.0_151"
$env:PATH = "$env:JAVA_HOME\bin;$env:PATH"

$MAVEN_PROJECTBASEDIR = Get-Location
$WRAPPER_JAR = "$MAVEN_PROJECTBASEDIR\.mvn\wrapper\maven-wrapper.jar"

java -Dmaven.multiModuleProjectDirectory="$MAVEN_PROJECTBASEDIR" -classpath "$WRAPPER_JAR" org.apache.maven.wrapper.MavenWrapperMain --version