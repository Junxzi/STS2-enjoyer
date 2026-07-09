#!/usr/bin/env bash
set -euo pipefail

configuration="${CONFIGURATION:-Debug}"
sts2_install_path="${STS2_INSTALL_PATH:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}"
resources_path="$sts2_install_path/SlayTheSpire2.app/Contents/Resources"
macos_path="$sts2_install_path/SlayTheSpire2.app/Contents/MacOS"
architecture="$(uname -m)"
data_path="$resources_path/data_sts2_macos_$architecture"
dotnet_cmd="${DOTNET_CMD:-/opt/homebrew/opt/dotnet@9/bin/dotnet}"

if [[ ! -x "$dotnet_cmd" ]]; then
  dotnet_cmd="dotnet"
fi

if [[ ! -d "$sts2_install_path" ]]; then
  echo "STS2 install path was not found: $sts2_install_path" >&2
  exit 1
fi

if [[ ! -f "$data_path/sts2.dll" ]]; then
  echo "STS2 data path was not found or missing sts2.dll: $data_path" >&2
  exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
party_race_root="$(cd "$script_dir/.." && pwd)"
mod_project="$party_race_root/src/PartyRace.Mod/PartyRace.Mod.csproj"
manifest_source="$party_race_root/src/PartyRace.Mod/mod_manifest.json"
mod_output="$party_race_root/src/PartyRace.Mod/bin/$configuration/net9.0"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-${TMPDIR:-/tmp}/party-race-dotnet-cli-home}"
mkdir -p "$DOTNET_CLI_HOME"

"$dotnet_cmd" build "$mod_project" --configuration "$configuration" "/p:Sts2DataPath=$data_path"

if [[ ! -f "$mod_output/PartyRace.Mod.dll" ]]; then
  echo "Mod build output was not found: $mod_output/PartyRace.Mod.dll" >&2
  exit 1
fi

deploy_one() {
  local mod_destination="$1"
  mkdir -p "$mod_destination"
  cp -f "$manifest_source" "$mod_destination/mod_manifest.json"
  cp -f "$mod_output/PartyRace.Mod.dll" "$mod_destination/party_race.dll"
  [[ -f "$mod_output/PartyRace.Mod.pdb" ]] && cp -f "$mod_output/PartyRace.Mod.pdb" "$mod_destination/party_race.pdb"
  cp -f "$mod_output/PartyRace.Core.dll" "$mod_destination/PartyRace.Core.dll"
  [[ -f "$mod_output/PartyRace.Core.pdb" ]] && cp -f "$mod_output/PartyRace.Core.pdb" "$mod_destination/PartyRace.Core.pdb"
  cp -f "$mod_output/PartyRace.Sts2Adapter.dll" "$mod_destination/PartyRace.Sts2Adapter.dll"
  [[ -f "$mod_output/PartyRace.Sts2Adapter.pdb" ]] && cp -f "$mod_output/PartyRace.Sts2Adapter.pdb" "$mod_destination/PartyRace.Sts2Adapter.pdb"
  echo "Deployed Party Race to $mod_destination"
}

deploy_one "$sts2_install_path/mods/party_race"
deploy_one "$resources_path/mods/party_race"
deploy_one "$macos_path/mods/party_race"

echo "Launch STS2, then check:"
echo "  test -f \"$HOME/Library/Application Support/PartyRace/party_race_mod_loaded.log\""
echo "  tail -n 80 \"$HOME/Library/Application Support/SlayTheSpire2/logs/godot.log\""
