// Demo data for modules not yet backed by real endpoints (CRM, Invoices, Projects, Help Desk
// aren't built yet — see RoadmapFinal.md). Isolated here so it's obvious what's real vs. demo,
// and so swapping to real API calls later means editing call sites, not hunting for inline mocks.

export const revenueTrend = [
  { month: "Feb", revenue: 42000 },
  { month: "Mar", revenue: 51000 },
  { month: "Apr", revenue: 47500 },
  { month: "May", revenue: 61000 },
  { month: "Jun", revenue: 58500 },
  { month: "Jul", revenue: 72000 },
];

export const demoKpis = {
  revenue: revenueTrend[revenueTrend.length - 1].revenue,
  activeProjects: 14,
  activeTickets: 23,
};

export const pipelineStages = [
  { stage: "New", count: 18, value: 145000 },
  { stage: "Qualified", count: 12, value: 210000 },
  { stage: "Proposal", count: 7, value: 168000 },
  { stage: "Negotiation", count: 4, value: 122000 },
  { stage: "Won", count: 9, value: 264000 },
];

export const recentActivity = [
  { id: "1", type: "deal", text: "Deal \"Acme Website Revamp\" moved to Negotiation", time: "12m ago" },
  { id: "2", type: "ticket", text: "Ticket #4821 assigned to Sara Hassan", time: "38m ago" },
  { id: "3", type: "quotation", text: "Quotation Q-2026-014 sent to Nexora Logistics", time: "1h ago" },
  { id: "4", type: "task", text: "Omar Fahmy completed \"Server migration\" task", time: "2h ago" },
  { id: "5", type: "customer", text: "New customer \"Delta Retail Group\" added", time: "4h ago" },
];

export const quickActions = [
  { label: "New Deal", href: "/crm/leads" },
  { label: "New Quotation", href: "/quotations/new" },
  { label: "New Ticket", href: "/helpdesk" },
  { label: "New Task", href: "/tasks/board" },
];

export const upcomingEvents = [
  { id: "1", title: "Client call — Nexora Logistics", time: "Today, 2:00 PM" },
  { id: "2", title: "Project kickoff — Delta Retail", time: "Today, 4:30 PM" },
  { id: "3", title: "Quarterly review", time: "Tomorrow, 10:00 AM" },
];
