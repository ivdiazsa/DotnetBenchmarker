#!/usr/bin/env bash

OUTPUT_DIR=lin-output-$COMPOSITES_TYPE
if [ ! -d "$OUTPUT_DIR" ]; then
  mkdir $OUTPUT_DIR
fi

DOTNET_PATH="DotnetLinux/dotnet$DOTNET_VERSION_NUMBER"
CORELIB_PATH=$(find $DOTNET_PATH/shared -name System.Private.CoreLib.dll)
ASPNET_PATH=$(find $DOTNET_PATH/shared -name Microsoft.AspNetCore.dll)

FX_PATH=$(dirname $CORELIB_PATH)
ASP_PATH=$(dirname $ASPNET_PATH)
CROSSGEN2_PATH='Crossgen2Linux/crossgen2'

BASE_CMD="$CROSSGEN2_PATH"
BASE_CMD+=" --targetos Linux"
BASE_CMD+=" --targetarch x64"

if [[ ${USE_AVX2,,} == "true" ]]; then
  echo "Will apply AVX2 Instruction Set..."
  BASE_CMD+=" --instruction-set:avx2"
  BASE_CMD+=" --inputbubble"
fi

# At least for the time being, we expect the optimization data MIBC file to be
# in the same directory as the Crossgen2 build.
if [ -f "Crossgen2Linux/StandardOptimizationData.mibc" ]; then
  echo "Will use StandardOptimizationData.mibc..."
  BASE_CMD+=" --mibc Crossgen2Linux/StandardOptimizationData.mibc"
fi

if [[ ${FRAMEWORK_COMPOSITE,,} == "true" ]]; then
  echo "Compiling Framework Composites..."
  COMPOSITE_FILE='framework'

  BUILDFX_CMD="$BASE_CMD"
  BUILDFX_CMD+=" --composite"

  ASSEMBLIES_TO_COMPOSITE=''
  if [[ ${PARTIAL_COMPOSITES,,} != "0" ]]; then
    while read -r line
    do
      ASSEMBLIES_TO_COMPOSITE+=" $FX_PATH/$line"
    done < "$PARTIAL_COMPOSITES"
    COMPOSITE_FILE+='-partial'
  else
    ASSEMBLIES_TO_COMPOSITE=" $FX_PATH/*.dll"
  fi

  BUILDFX_CMD+=" $ASSEMBLIES_TO_COMPOSITE"

  if [[ ${BUNDLE_ASPNET,,} == "true" ]]; then
    echo "ASP.NET will be bundled into the composite image..."
    BUILDFX_CMD+=" $ASP_PATH/*.dll"
    COMPOSITE_FILE+='-aspnet'
  fi

  BUILDFX_CMD+=" --out $OUTPUT_DIR/$COMPOSITE_FILE.r2r.dll"
  $BUILDFX_CMD

else
  # Iterate over each Framework Assembly and recompile it using Crossgen2.
  # Output the resulting images to the corresponding output folder.
  echo "Applying Crossgen2 normally..."

  for FILE in $FX_PATH/*.dll; do
    BUILDBIN_CMD="$BASE_CMD"
    BUILDBIN_CMD+=" --reference $FX_PATH/System.Private.CoreLib.dll"
    BUILDBIN_CMD+=" --reference $FX_PATH/System.Runtime.dll"
    BUILDBIN_CMD+=" $FILE"
    BUILDBIN_CMD+=" --out $OUTPUT_DIR/$(basename $FILE)"
    $BUILDBIN_CMD
  done

  # for FILE in $ASP_PATH/*.dll; do
  #   BUILDBIN_CMD="$BASE_CMD"
  #   BUILDBIN_CMD+=" --reference $FX_PATH/System.Private.CoreLib.dll"
  #   BUILDBIN_CMD+=" --reference $FX_PATH/System.Runtime.dll"
  #   BUILDBIN_CMD+=" $FILE"
  #   BUILDBIN_CMD+=" --out $OUTPUT_DIR/$(basename $FILE)"
  #   $BUILDBIN_CMD
  # done
fi

if [[ ${ASPNET_COMPOSITE,,} == "true" && ${BUNDLE_ASPNET,,} != "true" ]]; then
  echo "Compiling ASP.NET Framework Composites..."
  BUILDASPNET_CMD="$BASE_CMD"
  BUILDASPNET_CMD+=" --composite"
  BUILDASPNET_CMD+=" $ASP_PATH/*.dll"
  BUILDASPNET_CMD+=" --reference $FX_PATH/*.dll"
  BUILDASPNET_CMD+=" --out $OUTPUT_DIR/aspnetcore.r2r.dll"
  $BUILDASPNET_CMD
fi

# Copy the .so objects as well. Otherwise, the runtime doesn't know how to
# work with our newly created binaries.
cp $FX_PATH/*.so -t $OUTPUT_DIR
