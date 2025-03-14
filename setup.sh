#!/bin/bash

# Hybrid Connection Manager Service Installation Script
# This script installs the Hybrid Connection Manager as a systemd service on Linux

# Check if running as root
if [ "$EUID" -ne 0 ]; then
  echo "Please run as root or using sudo"
  exit 1
fi

echo "Adding hcm to path.."

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
export HCM=$SCRIPT_DIR/CLI
export PATH="$HCM:$PATH"

echo "Setting up hybridconnectionmanager.service.."

chmod 777 ./Service/linux/install_service.sh
./Service/linux/install_service.sh