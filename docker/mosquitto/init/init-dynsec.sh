#!/bin/sh
#
# Adds roles,accounts and groups to dynamic Security
# The admin account and default role are created automaticlly ba Mosquitto
# on the firt start using the MOSQUITTO_DYNSEC_PASSWORD veriable.
# The script is idempotent: on subsequent starts, commands for existing items siply fail and execution continues
ctl() {
    mosquitto_ctrl -h mosquitto -u admin -P "$MOSQUITTO_DYNSEC_PASSWORD" dynsec "$@" 2>&1 \
    | grep -v -e "without encryption" -e "configuration changes"
}

echo "Waiting for broker ..."

until mosquitto_ctrl -h mosquitto -u admin -P "$MOSQUITTO_DYNSEC_PASSWORD" dynsec getAnonymousGroup >/dev/null 2>&1; do
        sleep 1
done


# Roles for Anonymus device for claim (%c = client id = claim token / hardwareId)
ctl createRole claim
ctl addRoleACL claim publishClientSend    iot/v1/claim/request          allow
ctl addRoleACL claim publishClientReceive 'iot/v1/claim/response/%c'    allow
ctl addRoleACL claim subscribePattern     'iot/v1/claim/response/%c'    allow

# Roles for approving device (%u = username = hardwareId)
ctl createRole device
ctl addRoleACL device publishClientSend    'iot/v1/%u/telemetry' allow
ctl addRoleACL device publishClientSend    'iot/v1/%u/status'    allow
ctl addRoleACL device publishClientReceive 'iot/v1/%u/#'         allow
ctl addRoleACL device subscribePattern     'iot/v1/%u/config'    allow
ctl addRoleACL device subscribePattern     'iot/v1/%u/command'   allow

# Roles for backend
ctl createRole backend
ctl addRoleACL backend publishClientSend    'iot/v1/+/config'          allow
ctl addRoleACL backend publishClientSend    'iot/v1/+/command'         allow
ctl addRoleACL backend publishClientSend    'iot/v1/claim/response/+'  allow
ctl addRoleACL backend publishClientReceive 'iot/v1/#'                 allow
ctl addRoleACL backend subscribeLiteral     iot/v1/claim/request       allow
ctl addRoleACL backend subscribePattern     'iot/v1/+/telemetry'       allow
ctl addRoleACL backend subscribePattern     'iot/v1/+/status'          allow

ctl createGroup devices
ctl addGroupRole devices device
ctl createGroup services
ctl addGroupRole services backend
ctl addGroupRole unauthenticated claim
ctl setAnonymousGroup unauthenticated

# backend account (password from .env)
ctl createClient backend-api -p "$MQTT_BACKEND_PASSWORD"
ctl addGroupClient services backend-api
ctl createClient provisioner -p "$MQTT_PROVISIONER_PASSWORD"
ctl addClientRole provisioner dynsec-admin

echo "Dynamic Security ready."
