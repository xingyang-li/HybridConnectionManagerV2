#!/bin/bash

# Hybrid Connection Manager Service Installation Script
# This script installs the Hybrid Connection Manager as a systemd service on Linux

# Check if running as root
if [ "$EUID" -ne 0 ]; then
  echo "Please run as root or using sudo"
  exit 1
fi

SERVICE_NAME="hybridconnectionmanager.service"

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
APP_DIR="$(dirname "$SCRIPT_DIR")"
PROJ_DIR="$(dirname "$APP_DIR")"

# Default installation path
DEFAULT_INSTALL_PATH="/opt/$SERVICE_NAME"
INSTALL_PATH=$DEFAULT_INSTALL_PATH

# Default user (current user)
DEFAULT_USER=$(logname || echo $SUDO_USER || echo $USER)
SERVICE_USER=$DEFAULT_USER

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  key="$1"
  case $key in
    --path)
      INSTALL_PATH="$2"
      shift
      shift
      ;;
    --user)
      SERVICE_USER="$2"
      shift
      shift
      ;;
    --help)
      echo "Usage: $0 [options]"
      echo "Options:"
      echo "  --path PATH    Installation path (default: $DEFAULT_INSTALL_PATH)"
      echo "  --user USER    User to run the service as (default: current user)"
      echo "  --help         Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

echo "Removing previous installation of Hybrid Connection Manager Service..."
sudo systemctl stop $SERVICE_NAME
sudo systemctl disable $SERVICE_NAME
sudo rm "/etc/systemd/system/hybridconnectionmanager.service"

rm -rf $INSTALL_PATH

echo "Installing Hybrid Connection Manager Service..."
echo "Installation path: $INSTALL_PATH"
echo "Service user: $SERVICE_USER"

# Create installation directory
echo "Creating installation directory..."
mkdir -p $INSTALL_PATH

# Copy service files from application directory
echo "Copying service files..."
cp -R "$APP_DIR/"* "$INSTALL_PATH/"

# Set ownership and permissions
echo "Setting ownership and permissions..."
chown -R $SERVICE_USER:$SERVICE_USER $INSTALL_PATH
chmod -R 755 $INSTALL_PATH

# Create and configure the systemd service file
echo "Creating systemd service file..."
SERVICE_FILE="/etc/systemd/system/hybridconnectionmanager.service"
cp "$SCRIPT_DIR/hybridconnectionmanager.service" $SERVICE_FILE

# Replace placeholders in the service file
sed -i "s|__USER__|$SERVICE_USER|g" $SERVICE_FILE
sed -i "s|__INSTALL_PATH__|$INSTALL_PATH|g" $SERVICE_FILE

# Set proper permissions for service file
chmod 644 $SERVICE_FILE

# Reload systemd to recognize the new service
echo "Reloading systemd..."
systemctl daemon-reload

# Enable the service to start on boot
echo "Enabling service..."
systemctl enable hybridconnectionmanager.service

# Check if the user wants to start the service now
read -p "Do you want to start the service now? (y/n): " START_SERVICE
if [[ $START_SERVICE =~ ^[Yy]$ ]]; then
  echo "Starting service..."
  systemctl start $SERVICE_NAME
  echo "Checking service status..."
  systemctl status $SERVICE_NAME
else
  echo "Service installed but not started."
  echo "To start the service, run: sudo systemctl start $SERVICE_NAME"
fi

echo ""
echo "Installation complete!"
echo "You can manage the service with the following commands:"
echo "  - Start: sudo systemctl start $SERVICE_NAME"
echo "  - Stop: sudo systemctl stop $SERVICE_NAME"
echo "  - Restart: sudo systemctl $SERVICE_NAME"
echo "  - Status: sudo systemctl status $SERVICE_NAME"
echo "  - View logs: cd /usr/share/HybridConnectionManager/Logs"