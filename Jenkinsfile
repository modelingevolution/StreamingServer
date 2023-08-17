AGENT_LABEL='docker_worker'

pipeline {
    agent { label "${AGENT_LABEL}" }
    options {
        ansiColor('xterm')
    }


    stages {
        stage('Build and Deploy') {
            steps {
                echo 'Building.. and Deploying....'
                echo 'Entering the directory Build and starting PowerShell script..'
                sh "cd Deployment && \
                    pwsh -Command Invoke-Cake && \
                    pwsh -Command Invoke-Cake cake-deployer.ps1"

            }
        }
}