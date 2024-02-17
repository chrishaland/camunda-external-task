FROM ubuntu:20.04
ENV CAMUNDA_VERSION="7.20"
ENV CAMUNDA_HOME="/opt/camunda-7-run"
RUN apt update && \
    DEBIAN_FRONTEND=noninteractive apt install -y vim curl git zlib1g libstdc++6 libssl1.1 libgssapi-krb5-2 libgcc1 libc6 libicu66
RUN curl -L https://dot.net/v1/dotnet-install.sh -o ./dotnet-install.sh && \
    chmod +x ./dotnet-install.sh && \
    ./dotnet-install.sh --channel 8.0 && \
    ./dotnet-install.sh --channel 7.0 && \
    ./dotnet-install.sh --channel 6.0 && \
    echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc && \
    echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> ~/.bashrc && \
    rm ./dotnet-install.sh
RUN apt update && \
    apt install -y gpg && \
    curl https://apt.corretto.aws/corretto.key | gpg --dearmor -o /usr/share/keyrings/corretto-keyring.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/corretto-keyring.gpg] https://apt.corretto.aws stable main" | tee /etc/apt/sources.list.d/corretto.list && \
    apt update && \
    apt install -y java-17-amazon-corretto-jdk
RUN mkdir -p $CAMUNDA_HOME && \ 
    curl -L "https://downloads.camunda.cloud/release/camunda-bpm/run/${CAMUNDA_VERSION}/camunda-bpm-run-${CAMUNDA_VERSION}.0.tar.gz" | tar -xzf - -C $CAMUNDA_HOME
