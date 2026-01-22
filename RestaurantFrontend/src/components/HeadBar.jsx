import { useState } from "react";
import {
  VegaBox,
  VegaCard,
  VegaFlex,
  VegaFont,
  VegaInput,
  VegaInputSelect,
} from "@heartlandone/vega-react";
import Slider from "rc-slider";
import "rc-slider/assets/index.css";

const HeadBar = () => {
  const [rate, setRate] = useState(1);

  const marks = {
    0.5: "0.5x",
    0.75: "0.75x",
    1: "1x",
    1.25: "1.25x",
    1.5: "1.5x",
    2: "2x",
  };

  return (
    <VegaCard>
      <VegaFlex
        gap={"size-32"}
        direction={"col"}
        alignItems={"start"}
        style={{ padding: "0 auto" }}
      >
        <VegaFlex
          gap={"size-24"}
          alignItems="center"
          justifyContent="center"
          style={{ width: "30%" }}
        >
          <VegaFont variant="font-field-label" class="v-text-on-primary">
            Accelerate
          </VegaFont>
          <Slider
            min={0.5}
            max={2}
            marks={{ 0.5: "0.5x", 1: "1x", 1.5: "1.5x", 2: "2x" }}
            value={rate}
            onChange={(v) => {
              setRate(v);
            }}
            styles={{
              track: { backgroundColor: "#1677ff" },
              handle: { borderColor: "#1677ff" },
            }}
          />
        </VegaFlex>
        <VegaFlex gap={"size-24"} style={{ width: "60%" }}>
          <VegaInput size="small" label="Tables" />
          <VegaInput size="small" label="Bot" />
          <VegaInput size="small" label="Gusts" />
        </VegaFlex>
      </VegaFlex>
    </VegaCard>
  );
};

export default HeadBar;
