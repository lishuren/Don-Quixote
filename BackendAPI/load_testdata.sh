#!/bin/bash

#############################################################################
# Restaurant Robot Direction Assistant - Test Data Loader
# 
# This script loads all test data into the backend API.
# Usage: ./load_testdata.sh [BASE_URL]
#
# Default BASE_URL: http://localhost:5000/api
#############################################################################

set -e

# Configuration
BASE_URL="${1:-http://localhost:5000/api}"
CONTENT_TYPE="Content-Type: application/json"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
SUCCESS_COUNT=0
FAIL_COUNT=0

#############################################################################
# Helper Functions
#############################################################################

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
    ((SUCCESS_COUNT++))
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
    ((FAIL_COUNT++))
}

log_section() {
    echo ""
    echo -e "${YELLOW}========================================${NC}"
    echo -e "${YELLOW}$1${NC}"
    echo -e "${YELLOW}========================================${NC}"
}

# POST request helper
post_data() {
    local endpoint="$1"
    local data="$2"
    local description="$3"
    
    response=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}${endpoint}" \
        -H "${CONTENT_TYPE}" \
        -d "${data}" 2>/dev/null) || true
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    
    if [[ "$http_code" =~ ^2[0-9][0-9]$ ]]; then
        log_success "$description"
        return 0
    else
        log_error "$description (HTTP $http_code)"
        return 1
    fi
}

# PUT request helper
put_data() {
    local endpoint="$1"
    local data="$2"
    local description="$3"
    
    response=$(curl -s -w "\n%{http_code}" -X PUT "${BASE_URL}${endpoint}" \
        -H "${CONTENT_TYPE}" \
        -d "${data}" 2>/dev/null) || true
    
    http_code=$(echo "$response" | tail -n1)
    
    if [[ "$http_code" =~ ^2[0-9][0-9]$ ]]; then
        log_success "$description"
        return 0
    else
        log_error "$description (HTTP $http_code)"
        return 1
    fi
}

#############################################################################
# 1. Layout Configuration
#############################################################################

load_layout() {
    log_section "1. Loading Layout Configuration"
    
    post_data "/layout" '{
        "id": "layout_main_v1",
        "name": "Main Dining Hall",
        "coordinateMapping": {
            "scale": 0.05,
            "screenSize": { "width": 900, "height": 600 },
            "physicalSize": { "width": 45.0, "height": 30.0 },
            "gridResolution": 20
        },
        "floorId": "floor_1",
        "mapId": "map_restaurant_main"
    }' "Layout configuration"
}

#############################################################################
# 2. Robots (10 robots)
#############################################################################

load_robots() {
    log_section "2. Loading Robots (10 robots)"
    
    # Robot 1 - Don Quixote
    post_data "/robots" '{
        "id": "bot_1",
        "name": "Don Quixote",
        "serialNumber": "INBOT-DQ-001",
        "enabled": true,
        "state": "IDLE",
        "battery": 95,
        "position": { "screen": { "x": 50, "y": 550 }, "physical": { "x": 2.5, "y": 27.5 } },
        "heading": 0,
        "homePosition": { "screen": { "x": 50, "y": 550 }, "physical": { "x": 2.5, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_1"
    }' "Robot bot_1 (Don Quixote)"
    
    # Robot 2 - Sancho Panza
    post_data "/robots" '{
        "id": "bot_2",
        "name": "Sancho Panza",
        "serialNumber": "INBOT-SP-002",
        "enabled": true,
        "state": "MOVING",
        "battery": 88,
        "position": { "screen": { "x": 300, "y": 250 }, "physical": { "x": 15.0, "y": 12.5 } },
        "heading": 45,
        "homePosition": { "screen": { "x": 100, "y": 550 }, "physical": { "x": 5.0, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_2"
    }' "Robot bot_2 (Sancho Panza)"
    
    # Robot 3 - Dulcinea
    post_data "/robots" '{
        "id": "bot_3",
        "name": "Dulcinea",
        "serialNumber": "INBOT-DL-003",
        "enabled": true,
        "state": "CHARGING",
        "battery": 45,
        "position": { "screen": { "x": 150, "y": 550 }, "physical": { "x": 7.5, "y": 27.5 } },
        "heading": 0,
        "homePosition": { "screen": { "x": 150, "y": 550 }, "physical": { "x": 7.5, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT"],
        "speedLimit": 0.8,
        "sdkPointId": "charge_point_3"
    }' "Robot bot_3 (Dulcinea)"
    
    # Robot 4 - Rocinante
    post_data "/robots" '{
        "id": "bot_4",
        "name": "Rocinante",
        "serialNumber": "INBOT-RC-004",
        "enabled": true,
        "state": "IDLE",
        "battery": 92,
        "position": { "screen": { "x": 200, "y": 550 }, "physical": { "x": 10.0, "y": 27.5 } },
        "heading": 0,
        "homePosition": { "screen": { "x": 200, "y": 550 }, "physical": { "x": 10.0, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_4"
    }' "Robot bot_4 (Rocinante)"
    
    # Robot 5 - Clavileño
    post_data "/robots" '{
        "id": "bot_5",
        "name": "Clavileño",
        "serialNumber": "INBOT-CL-005",
        "enabled": true,
        "state": "MOVING",
        "battery": 78,
        "position": { "screen": { "x": 500, "y": 180 }, "physical": { "x": 25.0, "y": 9.0 } },
        "heading": 90,
        "homePosition": { "screen": { "x": 250, "y": 550 }, "physical": { "x": 12.5, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_5"
    }' "Robot bot_5 (Clavileño)"
    
    # Robot 6 - Barataria
    post_data "/robots" '{
        "id": "bot_6",
        "name": "Barataria",
        "serialNumber": "INBOT-BR-006",
        "enabled": true,
        "state": "IDLE",
        "battery": 85,
        "position": { "screen": { "x": 300, "y": 550 }, "physical": { "x": 15.0, "y": 27.5 } },
        "heading": 0,
        "homePosition": { "screen": { "x": 300, "y": 550 }, "physical": { "x": 15.0, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_6"
    }' "Robot bot_6 (Barataria)"
    
    # Robot 7 - Mambrino (BLOCKED)
    post_data "/robots" '{
        "id": "bot_7",
        "name": "Mambrino",
        "serialNumber": "INBOT-MB-007",
        "enabled": true,
        "state": "BLOCKED",
        "battery": 72,
        "position": { "screen": { "x": 340, "y": 195 }, "physical": { "x": 17.0, "y": 9.75 } },
        "heading": 135,
        "homePosition": { "screen": { "x": 350, "y": 550 }, "physical": { "x": 17.5, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT"],
        "speedLimit": 0.9,
        "sdkPointId": "charge_point_7",
        "blockedType": "TYPE_ENCOUNTER_OBSTACLES"
    }' "Robot bot_7 (Mambrino - BLOCKED)"
    
    # Robot 8 - Montesinos
    post_data "/robots" '{
        "id": "bot_8",
        "name": "Montesinos",
        "serialNumber": "INBOT-MT-008",
        "enabled": true,
        "state": "MOVING",
        "battery": 65,
        "position": { "screen": { "x": 700, "y": 350 }, "physical": { "x": 35.0, "y": 17.5 } },
        "heading": 270,
        "homePosition": { "screen": { "x": 400, "y": 550 }, "physical": { "x": 20.0, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_8"
    }' "Robot bot_8 (Montesinos)"
    
    # Robot 9 - Toboso (Low battery charging)
    post_data "/robots" '{
        "id": "bot_9",
        "name": "Toboso",
        "serialNumber": "INBOT-TB-009",
        "enabled": true,
        "state": "CHARGING",
        "battery": 22,
        "position": { "screen": { "x": 450, "y": 550 }, "physical": { "x": 22.5, "y": 27.5 } },
        "heading": 0,
        "homePosition": { "screen": { "x": 450, "y": 550 }, "physical": { "x": 22.5, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT"],
        "speedLimit": 0.8,
        "sdkPointId": "charge_point_9"
    }' "Robot bot_9 (Toboso - LOW BATTERY)"
    
    # Robot 10 - La Mancha (OFFLINE)
    post_data "/robots" '{
        "id": "bot_10",
        "name": "La Mancha",
        "serialNumber": "INBOT-LM-010",
        "enabled": false,
        "state": "OFFLINE",
        "battery": 0,
        "position": { "screen": { "x": 500, "y": 550 }, "physical": { "x": 25.0, "y": 27.5 } },
        "heading": 0,
        "homePosition": { "screen": { "x": 500, "y": 550 }, "physical": { "x": 25.0, "y": 27.5 } },
        "capabilities": ["DELIVERY", "ESCORT", "BUSSING"],
        "speedLimit": 1.0,
        "sdkPointId": "charge_point_10",
        "offlineReason": "MAINTENANCE"
    }' "Robot bot_10 (La Mancha - OFFLINE)"
}

#############################################################################
# 3. Tables (20 tables)
#############################################################################

load_tables() {
    log_section "3. Loading Tables (20 tables)"
    
    local tables=(
        '{"id":"table_1","label":"T1","type":"t2","capacity":2,"position":{"screen":{"x":150,"y":100},"physical":{"x":7.5,"y":5.0}},"zone":"window","state":"AVAILABLE","enabled":true,"reservable":true,"features":["window_view"],"sdkPointId":"table_point_1"}'
        '{"id":"table_2","label":"T2","type":"t2","capacity":2,"position":{"screen":{"x":250,"y":100},"physical":{"x":12.5,"y":5.0}},"zone":"window","state":"AVAILABLE","enabled":true,"reservable":true,"features":["window_view"],"sdkPointId":"table_point_2"}'
        '{"id":"table_3","label":"T3","type":"t4","capacity":4,"position":{"screen":{"x":350,"y":100},"physical":{"x":17.5,"y":5.0}},"zone":"window","state":"OCCUPIED_DINING","enabled":true,"reservable":true,"features":["window_view"],"sdkPointId":"table_point_3"}'
        '{"id":"table_4","label":"T4","type":"t4","capacity":4,"position":{"screen":{"x":450,"y":100},"physical":{"x":22.5,"y":5.0}},"zone":"window","state":"RESERVED","enabled":true,"reservable":true,"features":["window_view","quiet"],"sdkPointId":"table_point_4"}'
        '{"id":"table_5","label":"T5","type":"t6","capacity":6,"position":{"screen":{"x":600,"y":100},"physical":{"x":30.0,"y":5.0}},"zone":"window","state":"AVAILABLE","enabled":true,"reservable":true,"features":["window_view","large_party"],"sdkPointId":"table_point_5"}'
        '{"id":"table_6","label":"T6","type":"t2","capacity":2,"position":{"screen":{"x":150,"y":200},"physical":{"x":7.5,"y":10.0}},"zone":"main","state":"DIRTY","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_6"}'
        '{"id":"table_7","label":"T7","type":"t4","capacity":4,"position":{"screen":{"x":250,"y":200},"physical":{"x":12.5,"y":10.0}},"zone":"main","state":"CLEANING","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_7"}'
        '{"id":"table_8","label":"T8","type":"t4","capacity":4,"position":{"screen":{"x":350,"y":200},"physical":{"x":17.5,"y":10.0}},"zone":"main","state":"AVAILABLE","enabled":true,"reservable":true,"features":["booth"],"sdkPointId":"table_point_8"}'
        '{"id":"table_9","label":"T9","type":"t4","capacity":4,"position":{"screen":{"x":450,"y":200},"physical":{"x":22.5,"y":10.0}},"zone":"main","state":"OCCUPIED_SEATED","enabled":true,"reservable":true,"features":["booth"],"sdkPointId":"table_point_9"}'
        '{"id":"table_10","label":"T10","type":"t8","capacity":8,"position":{"screen":{"x":600,"y":200},"physical":{"x":30.0,"y":10.0}},"zone":"main","state":"AVAILABLE","enabled":true,"reservable":true,"features":["large_party","round_table"],"sdkPointId":"table_point_10"}'
        '{"id":"table_11","label":"T11","type":"t2","capacity":2,"position":{"screen":{"x":150,"y":300},"physical":{"x":7.5,"y":15.0}},"zone":"center","state":"AVAILABLE","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_11"}'
        '{"id":"table_12","label":"T12","type":"t4","capacity":4,"position":{"screen":{"x":250,"y":300},"physical":{"x":12.5,"y":15.0}},"zone":"center","state":"DO_NOT_DISTURB","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_12"}'
        '{"id":"table_13","label":"T13","type":"t4","capacity":4,"position":{"screen":{"x":350,"y":300},"physical":{"x":17.5,"y":15.0}},"zone":"center","state":"AVAILABLE","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_13"}'
        '{"id":"table_14","label":"T14","type":"t4","capacity":4,"position":{"screen":{"x":450,"y":300},"physical":{"x":22.5,"y":15.0}},"zone":"center","state":"AVAILABLE","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_14"}'
        '{"id":"table_15","label":"T15","type":"t6","capacity":6,"position":{"screen":{"x":600,"y":300},"physical":{"x":30.0,"y":15.0}},"zone":"center","state":"OUT_OF_SERVICE","enabled":false,"reservable":false,"features":["large_party"],"sdkPointId":"table_point_15"}'
        '{"id":"table_16","label":"T16","type":"t2","capacity":2,"position":{"screen":{"x":150,"y":400},"physical":{"x":7.5,"y":20.0}},"zone":"bar","state":"AVAILABLE","enabled":true,"reservable":true,"features":["bar_view"],"sdkPointId":"table_point_16"}'
        '{"id":"table_17","label":"T17","type":"t2","capacity":2,"position":{"screen":{"x":250,"y":400},"physical":{"x":12.5,"y":20.0}},"zone":"bar","state":"AVAILABLE","enabled":true,"reservable":true,"features":["bar_view"],"sdkPointId":"table_point_17"}'
        '{"id":"table_18","label":"T18","type":"t4","capacity":4,"position":{"screen":{"x":350,"y":400},"physical":{"x":17.5,"y":20.0}},"zone":"bar","state":"AVAILABLE","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_18"}'
        '{"id":"table_19","label":"T19","type":"t4","capacity":4,"position":{"screen":{"x":450,"y":400},"physical":{"x":22.5,"y":20.0}},"zone":"bar","state":"AVAILABLE","enabled":true,"reservable":true,"features":[],"sdkPointId":"table_point_19"}'
        '{"id":"table_20","label":"VIP","type":"t10","capacity":10,"position":{"screen":{"x":750,"y":300},"physical":{"x":37.5,"y":15.0}},"zone":"vip","state":"AVAILABLE","enabled":true,"reservable":true,"features":["vip","private","large_party"],"sdkPointId":"table_point_vip"}'
    )
    
    for i in "${!tables[@]}"; do
        post_data "/tables" "${tables[$i]}" "Table $((i+1)) of 20"
    done
}

#############################################################################
# 4. Checkpoints
#############################################################################

load_checkpoints() {
    log_section "4. Loading Checkpoints"
    
    post_data "/checkpoints" '{
        "id": "kitchen",
        "name": "Kitchen",
        "type": "SERVICE_HUB",
        "position": { "screen": { "x": 50, "y": 50 }, "physical": { "x": 2.5, "y": 2.5 } },
        "functions": ["FOOD_PICKUP", "DISH_RETURN"],
        "sdkPointId": "kitchen_point",
        "sdkPointType": "TARGET_POINT_TYPE_SHIPMENT"
    }' "Checkpoint: Kitchen"
    
    post_data "/checkpoints" '{
        "id": "reception",
        "name": "Reception",
        "type": "GUEST_ENTRY",
        "position": { "screen": { "x": 850, "y": 50 }, "physical": { "x": 42.5, "y": 2.5 } },
        "functions": ["GUEST_CHECKIN", "ESCORT_START"],
        "sdkPointId": "reception_point",
        "sdkPointType": "TARGET_POINT_TYPE_COMMON"
    }' "Checkpoint: Reception"
    
    post_data "/checkpoints" '{
        "id": "cashier",
        "name": "Cashier",
        "type": "SERVICE_HUB",
        "position": { "screen": { "x": 850, "y": 550 }, "physical": { "x": 42.5, "y": 27.5 } },
        "functions": ["PAYMENT"],
        "sdkPointId": "cashier_point",
        "sdkPointType": "TARGET_POINT_TYPE_COMMON"
    }' "Checkpoint: Cashier"
    
    post_data "/checkpoints" '{
        "id": "bar",
        "name": "Bar Counter",
        "type": "SERVICE_HUB",
        "position": { "screen": { "x": 100, "y": 400 }, "physical": { "x": 5.0, "y": 20.0 } },
        "functions": ["DRINK_PICKUP"],
        "sdkPointId": "bar_point",
        "sdkPointType": "TARGET_POINT_TYPE_COMMON"
    }' "Checkpoint: Bar"
    
    post_data "/checkpoints" '{
        "id": "entrance",
        "name": "Main Entrance",
        "type": "ENTRANCE",
        "position": { "screen": { "x": 850, "y": 300 }, "physical": { "x": 42.5, "y": 15.0 } },
        "functions": ["ENTRANCE_CONTROL"],
        "sdkPointId": "entrance_point",
        "sdkPointType": "TARGET_POINT_TYPE_ENTRANCE"
    }' "Checkpoint: Main Entrance"
    
    # Charging stations
    for i in {1..10}; do
        local x=$((50 + (i-1) * 50))
        local px=$(echo "scale=1; 2.5 + ($i-1) * 2.5" | bc)
        post_data "/checkpoints" "{
            \"id\": \"charger_$i\",
            \"name\": \"Charging Station $i\",
            \"type\": \"ROBOT_DOCK\",
            \"position\": { \"screen\": { \"x\": $x, \"y\": 550 }, \"physical\": { \"x\": $px, \"y\": 27.5 } },
            \"functions\": [\"CHARGING\", \"IDLE_PARKING\"],
            \"capacity\": 1,
            \"sdkPointId\": \"charge_point_$i\",
            \"sdkPointType\": \"TARGET_POINT_TYPE_BASE_CHARGE\"
        }" "Checkpoint: Charger $i"
    done
}

#############################################################################
# 5. Zones
#############################################################################

load_zones() {
    log_section "5. Loading Zones"
    
    # Semantic Zones
    post_data "/zones/semantic" '{
        "id": "window",
        "name": "Window Section",
        "tableIds": ["table_1", "table_2", "table_3", "table_4", "table_5"]
    }' "Semantic Zone: Window"
    
    post_data "/zones/semantic" '{
        "id": "main",
        "name": "Main Hall",
        "tableIds": ["table_6", "table_7", "table_8", "table_9", "table_10"]
    }' "Semantic Zone: Main"
    
    post_data "/zones/semantic" '{
        "id": "center",
        "name": "Center Section",
        "tableIds": ["table_11", "table_12", "table_13", "table_14", "table_15"]
    }' "Semantic Zone: Center"
    
    post_data "/zones/semantic" '{
        "id": "bar",
        "name": "Bar Area",
        "tableIds": ["table_16", "table_17", "table_18", "table_19"]
    }' "Semantic Zone: Bar"
    
    post_data "/zones/semantic" '{
        "id": "vip",
        "name": "VIP Room",
        "tableIds": ["table_20"]
    }' "Semantic Zone: VIP"
    
    # Restricted Zones
    post_data "/zones/restricted" '{
        "id": "zone_001",
        "label": "Wet Floor - Bar Spill",
        "type": "WET_FLOOR",
        "active": true,
        "bounds": {
            "screen": { "x": 80, "y": 380, "width": 60, "height": 50 },
            "physical": { "x": 4.0, "y": 19.0, "width": 3.0, "height": 2.5 }
        },
        "expiresAt": "2026-01-26T12:45:00Z",
        "createdBy": "staff_john"
    }' "Restricted Zone: Wet Floor"
    
    post_data "/zones/restricted" '{
        "id": "zone_002",
        "label": "Maintenance - Floor Repair",
        "type": "MAINTENANCE",
        "active": true,
        "bounds": {
            "screen": { "x": 580, "y": 280, "width": 80, "height": 60 },
            "physical": { "x": 29.0, "y": 14.0, "width": 4.0, "height": 3.0 }
        },
        "expiresAt": "2026-01-26T14:00:00Z",
        "createdBy": "manager_sarah"
    }' "Restricted Zone: Maintenance"
}

#############################################################################
# 6. Guests (100 guests)
#############################################################################

load_guests() {
    log_section "6. Loading Guests (100 guests)"
    
    # Guests with special states (first 20)
    local special_guests=(
        '{"id":"guest_101","partySize":2,"arrivalTime":"2026-01-26T12:00:00Z","state":"QUEUED","queuePosition":1,"waitMinutes":0,"preferences":["window"]}'
        '{"id":"guest_102","partySize":4,"arrivalTime":"2026-01-26T12:02:00Z","state":"QUEUED","queuePosition":2,"waitMinutes":2,"preferences":["booth","quiet"],"notes":"Birthday celebration"}'
        '{"id":"guest_103","partySize":6,"arrivalTime":"2026-01-26T12:05:00Z","state":"ESCORTING","preferences":["large_party"],"reservationId":"res_201","assignedTableId":"table_5","escortRobotId":"bot_5"}'
        '{"id":"guest_104","partySize":4,"arrivalTime":"2026-01-26T11:30:00Z","state":"EATING","assignedTableId":"table_3","seatedAt":"2026-01-26T11:35:00Z"}'
        '{"id":"guest_105","partySize":2,"arrivalTime":"2026-01-26T11:45:00Z","state":"ORDERING","assignedTableId":"table_9","seatedAt":"2026-01-26T11:50:00Z"}'
        '{"id":"guest_106","partySize":8,"arrivalTime":"2026-01-26T11:42:00Z","state":"QUEUED","queuePosition":3,"waitMinutes":18,"preferences":["large_party","vip"],"notes":"Waiting for VIP table"}'
        '{"id":"guest_107","partySize":2,"arrivalTime":"2026-01-26T11:00:00Z","state":"PAYING","assignedTableId":"table_1","seatedAt":"2026-01-26T11:05:00Z"}'
        '{"id":"guest_108","partySize":4,"arrivalTime":"2026-01-26T10:30:00Z","state":"LINGERING","assignedTableId":"table_12","seatedAt":"2026-01-26T10:35:00Z","lingeringMinutes":20}'
        '{"id":"guest_109","partySize":3,"arrivalTime":"2026-01-26T12:08:00Z","state":"QUEUED","queuePosition":4,"waitMinutes":0,"preferences":[]}'
        '{"id":"guest_110","partySize":2,"arrivalTime":"2026-01-26T11:20:00Z","state":"EATING","assignedTableId":"table_2","seatedAt":"2026-01-26T11:22:00Z"}'
        '{"id":"guest_111","partySize":4,"arrivalTime":"2026-01-26T11:15:00Z","state":"EATING","assignedTableId":"table_8","seatedAt":"2026-01-26T11:18:00Z"}'
        '{"id":"guest_112","partySize":2,"arrivalTime":"2026-01-26T12:10:00Z","state":"QUEUED","queuePosition":5,"waitMinutes":0,"preferences":["bar_view"]}'
        '{"id":"guest_113","partySize":5,"arrivalTime":"2026-01-26T11:50:00Z","state":"ORDERING","assignedTableId":"table_10","seatedAt":"2026-01-26T11:55:00Z"}'
        '{"id":"guest_114","partySize":2,"arrivalTime":"2026-01-26T11:40:00Z","state":"EATING","assignedTableId":"table_11","seatedAt":"2026-01-26T11:42:00Z"}'
        '{"id":"guest_115","partySize":4,"arrivalTime":"2026-01-26T12:12:00Z","state":"QUEUED","queuePosition":6,"waitMinutes":0,"preferences":["booth"]}'
        '{"id":"guest_116","partySize":3,"arrivalTime":"2026-01-26T11:25:00Z","state":"EATING","assignedTableId":"table_13","seatedAt":"2026-01-26T11:28:00Z"}'
        '{"id":"guest_117","partySize":2,"arrivalTime":"2026-01-26T11:55:00Z","state":"ESCORTING","assignedTableId":"table_16","escortRobotId":"bot_2"}'
        '{"id":"guest_118","partySize":6,"arrivalTime":"2026-01-26T12:15:00Z","state":"QUEUED","queuePosition":7,"waitMinutes":0,"preferences":["large_party","window"]}'
        '{"id":"guest_119","partySize":2,"arrivalTime":"2026-01-26T11:10:00Z","state":"PAYING","assignedTableId":"table_17","seatedAt":"2026-01-26T11:12:00Z"}'
        '{"id":"guest_120","partySize":4,"arrivalTime":"2026-01-26T11:35:00Z","state":"EATING","assignedTableId":"table_14","seatedAt":"2026-01-26T11:38:00Z"}'
    )
    
    for guest in "${special_guests[@]}"; do
        local id=$(echo "$guest" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        post_data "/guests" "$guest" "Guest $id"
    done
    
    # Generate remaining 80 guests (121-200) - all QUEUED
    log_info "Generating guests 121-200 (QUEUED)..."
    local party_sizes=(2 3 4 5 6 2 2 3 4 4)
    local prefs_list=('["window"]' '["booth"]' '["quiet"]' '["large_party"]' '["bar_view"]' '[]' '["booth","quiet"]' '["window","vip"]' '["large_party","window"]' '[]')
    
    for i in $(seq 121 200); do
        local queue_pos=$((i - 112))
        local idx=$(( (i - 121) % 10 ))
        local party=${party_sizes[$idx]}
        local prefs=${prefs_list[$idx]}
        local hour=$(( 12 + (i - 121) / 30 ))
        local minute=$(( ((i - 121) % 30) * 2 ))
        local time=$(printf "2026-01-26T%02d:%02d:00Z" $hour $minute)
        
        post_data "/guests" "{
            \"id\": \"guest_$i\",
            \"partySize\": $party,
            \"arrivalTime\": \"$time\",
            \"state\": \"QUEUED\",
            \"queuePosition\": $queue_pos,
            \"waitMinutes\": 0,
            \"preferences\": $prefs
        }" "Guest guest_$i"
    done
}

#############################################################################
# 7. Reservations
#############################################################################

load_reservations() {
    log_section "7. Loading Reservations"
    
    post_data "/reservations" '{
        "id": "res_201",
        "partySize": 6,
        "dateTime": "2026-01-26T12:00:00Z",
        "name": "Johnson",
        "phone": "+1-555-0101",
        "email": "johnson@email.com",
        "preferences": ["large_party", "window"],
        "notes": "Anniversary dinner",
        "status": "CHECKED_IN",
        "tableId": "table_5",
        "guestId": "guest_103"
    }' "Reservation: Johnson (CHECKED_IN)"
    
    post_data "/reservations" '{
        "id": "res_202",
        "partySize": 4,
        "dateTime": "2026-01-26T12:30:00Z",
        "name": "Smith",
        "phone": "+1-555-0102",
        "email": "smith@email.com",
        "preferences": ["quiet", "booth"],
        "status": "CONFIRMED",
        "tableId": "table_4"
    }' "Reservation: Smith (CONFIRMED)"
    
    post_data "/reservations" '{
        "id": "res_203",
        "partySize": 2,
        "dateTime": "2026-01-26T13:00:00Z",
        "name": "Williams",
        "phone": "+1-555-0103",
        "preferences": ["window"],
        "notes": "First visit",
        "status": "CONFIRMED"
    }' "Reservation: Williams (CONFIRMED)"
    
    post_data "/reservations" '{
        "id": "res_204",
        "partySize": 8,
        "dateTime": "2026-01-26T19:00:00Z",
        "name": "Chen",
        "phone": "+1-555-0104",
        "email": "chen@company.com",
        "preferences": ["vip", "private"],
        "notes": "Business dinner - CEO hosting",
        "status": "CONFIRMED",
        "tableId": "table_20"
    }' "Reservation: Chen (CONFIRMED - VIP)"
    
    post_data "/reservations" '{
        "id": "res_205",
        "partySize": 4,
        "dateTime": "2026-01-26T11:00:00Z",
        "name": "Garcia",
        "phone": "+1-555-0105",
        "status": "NO_SHOW",
        "tableId": "table_8",
        "noShowAt": "2026-01-26T11:15:00Z"
    }' "Reservation: Garcia (NO_SHOW)"
}

#############################################################################
# 8. Tasks
#############################################################################

load_tasks() {
    log_section "8. Loading Tasks"
    
    post_data "/tasks" '{
        "taskId": "task_101",
        "type": "DELIVERY",
        "priority": "HIGH",
        "status": "IN_PROGRESS",
        "targetId": "table_3",
        "target": { "screen": { "x": 350, "y": 100 }, "physical": { "x": 17.5, "y": 5.0 } },
        "assignedRobotId": "bot_1",
        "payload": {
            "orderId": "order_501",
            "items": ["Grilled Salmon", "Caesar Salad", "Sparkling Water"]
        },
        "sdkTargetPointId": "table_point_3"
    }' "Task: DELIVERY to table_3 (IN_PROGRESS)"
    
    post_data "/tasks" '{
        "taskId": "task_102",
        "type": "ESCORT",
        "priority": "NORMAL",
        "status": "ASSIGNED",
        "targetId": "table_5",
        "target": { "screen": { "x": 600, "y": 100 }, "physical": { "x": 30.0, "y": 5.0 } },
        "assignedRobotId": "bot_2",
        "payload": {
            "guestId": "guest_103",
            "partySize": 6
        },
        "sdkTargetPointId": "table_point_5"
    }' "Task: ESCORT to table_5 (ASSIGNED)"
    
    post_data "/tasks" '{
        "taskId": "task_103",
        "type": "BUSSING",
        "priority": "LOW",
        "status": "PENDING",
        "targetId": "table_6",
        "target": { "screen": { "x": 150, "y": 200 }, "physical": { "x": 7.5, "y": 10.0 } },
        "queuePosition": 1,
        "sdkTargetPointId": "table_point_6"
    }' "Task: BUSSING at table_6 (PENDING)"
    
    post_data "/tasks" '{
        "taskId": "task_104",
        "type": "DELIVERY",
        "priority": "CRITICAL",
        "status": "PENDING",
        "targetId": "table_20",
        "target": { "screen": { "x": 750, "y": 300 }, "physical": { "x": 37.5, "y": 15.0 } },
        "queuePosition": 2,
        "payload": {
            "orderId": "order_502",
            "items": ["VIP Tasting Menu", "Dom Pérignon"],
            "notes": "VIP Guest - Top Priority"
        },
        "sdkTargetPointId": "table_point_vip"
    }' "Task: DELIVERY to VIP table (CRITICAL)"
    
    post_data "/tasks" '{
        "taskId": "task_105",
        "type": "RETURN_TO_DOCK",
        "priority": "LOW",
        "status": "PENDING",
        "targetId": "charger_3",
        "target": { "screen": { "x": 150, "y": 550 }, "physical": { "x": 7.5, "y": 27.5 } },
        "assignedRobotId": "bot_3",
        "payload": {
            "reason": "LOW_BATTERY",
            "batteryLevel": 15
        },
        "sdkTargetPointId": "charge_point_3"
    }' "Task: RETURN_TO_DOCK for bot_3"
}

#############################################################################
# 9. Alerts
#############################################################################

load_alerts() {
    log_section "9. Loading Alerts"
    
    post_data "/alerts" '{
        "id": "alert_501",
        "severity": "CRITICAL",
        "category": "ROBOT_STUCK",
        "title": "Robot bot_7 stuck for 3+ minutes",
        "message": "Bot_7 has been blocked near Table 8 for over 3 minutes. Manual assistance required.",
        "status": "ACTIVE",
        "context": {
            "robotId": "bot_7",
            "taskId": "task_106",
            "position": { "screen": { "x": 340, "y": 195 }, "physical": { "x": 17.0, "y": 9.75 } },
            "blockedDuration": 185,
            "blockedType": "TYPE_ENCOUNTER_OBSTACLES"
        },
        "suggestedActions": [
            "Clear obstacle near robot",
            "Manually push robot to safe zone",
            "Cancel current task and reassign"
        ]
    }' "Alert: ROBOT_STUCK (CRITICAL)"
    
    post_data "/alerts" '{
        "id": "alert_502",
        "severity": "HIGH",
        "category": "GUEST_WAITING",
        "title": "Guest waiting 18+ minutes",
        "message": "Guest party of 8 has been waiting for 18 minutes. Consider priority seating.",
        "status": "ACKNOWLEDGED",
        "acknowledgedBy": "staff_jane",
        "context": {
            "guestId": "guest_106",
            "partySize": 8,
            "waitMinutes": 18,
            "queuePosition": 1
        },
        "suggestedActions": [
            "Offer complimentary drinks",
            "Prepare large table",
            "Consider splitting party"
        ]
    }' "Alert: GUEST_WAITING (HIGH)"
    
    post_data "/alerts" '{
        "id": "alert_503",
        "severity": "MEDIUM",
        "category": "BATTERY_CRITICAL",
        "title": "Robot bot_9 battery at 22%",
        "message": "Bot_9 battery is critically low. Robot is charging.",
        "status": "ACTIVE",
        "context": {
            "robotId": "bot_9",
            "batteryLevel": 22,
            "estimatedRemainingMinutes": 15
        },
        "suggestedActions": [
            "Ensure charging dock is available",
            "Redistribute pending tasks"
        ]
    }' "Alert: BATTERY_CRITICAL (MEDIUM)"
    
    post_data "/alerts" '{
        "id": "alert_504",
        "severity": "LOW",
        "category": "GUEST_LINGERING",
        "title": "Table 12 guest lingering 20+ minutes",
        "message": "Guests at Table 12 have finished paying but remain seated for 20 minutes.",
        "status": "ACTIVE",
        "context": {
            "tableId": "table_12",
            "guestId": "guest_108",
            "lingeringMinutes": 20
        },
        "suggestedActions": [
            "Politely check if guests need anything",
            "Offer dessert menu if not ordered"
        ]
    }' "Alert: GUEST_LINGERING (LOW)"
}

#############################################################################
# Main Execution
#############################################################################

main() {
    echo ""
    echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║  Restaurant Robot Direction Assistant - Test Data Loader       ║${NC}"
    echo -e "${BLUE}╠════════════════════════════════════════════════════════════════╣${NC}"
    echo -e "${BLUE}║  Target: ${NC}${BASE_URL}"
    echo -e "${BLUE}║  Date:   ${NC}$(date '+%Y-%m-%d %H:%M:%S')"
    echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
    
    # Check if API is reachable
    log_info "Checking API connectivity..."
    if ! curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/health" 2>/dev/null | grep -q "200\|404"; then
        log_error "Cannot reach API at ${BASE_URL}"
        echo -e "${YELLOW}Make sure the backend server is running.${NC}"
        exit 1
    fi
    log_success "API is reachable"
    
    # Load all data
    load_layout
    load_robots
    load_tables
    load_checkpoints
    load_zones
    load_guests
    load_reservations
    load_tasks
    load_alerts
    
    # Summary
    log_section "Summary"
    echo -e "  ${GREEN}Successful:${NC} $SUCCESS_COUNT"
    echo -e "  ${RED}Failed:${NC}     $FAIL_COUNT"
    echo ""
    
    if [ $FAIL_COUNT -eq 0 ]; then
        echo -e "${GREEN}✓ All test data loaded successfully!${NC}"
        exit 0
    else
        echo -e "${YELLOW}⚠ Some operations failed. Check the errors above.${NC}"
        exit 1
    fi
}

# Run main function
main "$@"
