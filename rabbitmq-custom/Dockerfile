FROM rabbitmq:3.13-management-alpine

ARG RABBITMQ_VERSION=3.13.0
ARG PLUGIN_VERSION=3.13.0

RUN wget https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${PLUGIN_VERSION}/rabbitmq_delayed_message_exchange-${PLUGIN_VERSION}.ez \
    -P /opt/rabbitmq/plugins/

RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange rabbitmq_management