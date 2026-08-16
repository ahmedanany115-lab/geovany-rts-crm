import { pipelineStages } from "../data/mockData";

const stageColors = [
  "bg-slate-400",
  "bg-blue-400",
  "bg-indigo-400",
  "bg-amber-400",
  "bg-emerald text-emerald-foreground",
];

export function PipelineWidget() {
  const maxCount = Math.max(...pipelineStages.map((s) => s.count));

  return (
    <div className="rounded-lg border bg-background p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-sm font-semibold">Sales Pipeline</h2>
        <span className="rounded-full bg-accent px-2 py-0.5 text-xs text-muted-foreground">
          Demo data
        </span>
      </div>
      <div className="space-y-3">
        {pipelineStages.map((stage, i) => (
          <div key={stage.stage}>
            <div className="mb-1 flex items-center justify-between text-xs">
              <span className="font-medium">{stage.stage}</span>
              <span className="text-muted-foreground">
                {stage.count} · ${(stage.value / 1000).toFixed(0)}k
              </span>
            </div>
            <div className="h-2 w-full overflow-hidden rounded-full bg-accent">
              <div
                className={`h-full rounded-full ${stageColors[i % stageColors.length]}`}
                style={{ width: `${(stage.count / maxCount) * 100}%` }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
