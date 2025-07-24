################################################################################
# Copyright 2025 INTRIG
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
################################################################################

# Libraries
import pandas as pd
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
from datetime import datetime

# -----------------------------------------------------------------------------------------------------

# Explicit list of CSV files (without extension)
csv_filenames = ["MissionsLog_DRO001A_20250724_101824", "MissionsLog_DRO002B_20250724_101824", "MissionsLog_CAR003C_20250724_101824"]

# -----------------------------------------------------------------------------------------------------

# Dictionary to store data by player
players_data = {}

for filename in csv_filenames:
    file = filename + ".csv"
    df = pd.read_csv(file)

    # Convert CurrentTime to datetime format
    df['CurrentTime'] = pd.to_datetime(df['CurrentTime'], format='%H:%M:%S.%f')

    # Create a "Seconds" column relative to the first time value
    df['Seconds'] = (df['CurrentTime'] - df['CurrentTime'].iloc[0]).dt.total_seconds().astype(int)

    # Save the DataFrame using the player's name
    player_name = df['PlayerName'].iloc[0]
    players_data[player_name] = df

# -----------------------------------------------------------------------------------------------------

# 3D PLOT: Trajectories
fig = plt.figure(figsize=(10, 8))
ax = fig.add_subplot(111, projection='3d')

# Plot each player's trajectory in 3D
for player, df in players_data.items():
    ax.plot(df['Latitude'], df['Longitude'], df['Altitude'], label=player)

# Set the title and labels
ax.set_title("Players' 3D Trajectories")
ax.set_xlabel("Latitude")
ax.set_ylabel("Longitude")
ax.set_zlabel("Altitude")
ax.legend()
plt.tight_layout()

# Save the plot as a PNG file
plt.savefig('CarAndDrones_Trajectories.png', dpi=300)

# -----------------------------------------------------------------------------------------------------

# 2D PLOT: Battery Level
plt.figure(figsize=(10, 8))

# Plot each player's battery level over time
for player, df in players_data.items():
    plt.plot(df['Seconds'], df['BatteryLevel'], label=player)
    plt.fill_between(df['Seconds'], df['BatteryLevel'], alpha=0.3)  # Fill the area

# Set the title and labels
plt.xlabel("Time (s)")
plt.ylabel("Battery Level (%)")
plt.legend()
plt.grid(True)
plt.tight_layout()

# Save the plot as a PNG file
plt.savefig('CarAndDrones_BatteryLevel.png', dpi=300)

# -----------------------------------------------------------------------------------------------------

# Display all plots
plt.show()
