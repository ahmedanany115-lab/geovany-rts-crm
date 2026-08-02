"use client";

import {
    Users,
    DollarSign,
    Briefcase,
    TrendingUp,
    ArrowUpRight,
    Clock,
} from "lucide-react";

const stats = [
    {
        title: "Total Revenue",
        value: "$1,284,000",
        change: "+18.2%",
        icon: DollarSign,
    },
    {
        title: "Customers",
        value: "248",
        change: "+24",
        icon: Users,
    },
    {
        title: "Active Projects",
        value: "37",
        change: "+5",
        icon: Briefcase,
    },
    {
        title: "Conversion Rate",
        value: "42%",
        change: "+6.3%",
        icon: TrendingUp,
    },
];

export default function DashboardPage() {
    return (
        <div className="space-y-8">

            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold">
                        Welcome to Royal CRM
                    </h1>

                    <p className="mt-2 text-gray-500">
                        Executive overview of your company performance.
                    </p>
                </div>

                <button className="rounded-xl bg-blue-600 px-5 py-3 font-medium text-white hover:bg-blue-700">
                    Generate Report
                </button>
            </div>

            {/* KPI Cards */}

            <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-4">

                {stats.map((card) => {
                    const Icon = card.icon;

                    return (
                        <div
                            key={card.title}
                            className="rounded-2xl border bg-white p-6 shadow-sm transition hover:shadow-lg"
                        >
                            <div className="flex items-center justify-between">

                                <div>
                                    <p className="text-sm text-gray-500">
                                        {card.title}
                                    </p>

                                    <h2 className="mt-2 text-3xl font-bold">
                                        {card.value}
                                    </h2>

                                    <div className="mt-3 flex items-center gap-1 text-green-600">

                                        <ArrowUpRight size={16} />

                                        <span className="text-sm font-semibold">
                                            {card.change}
                                        </span>

                                    </div>
                                </div>

                                <div className="rounded-2xl bg-blue-100 p-4">
                                    <Icon className="h-8 w-8 text-blue-600" />
                                </div>

                            </div>
                        </div>
                    );
                })}
            </div>

            {/* Charts Placeholder */}

            <div className="grid gap-6 lg:grid-cols-3">

                <div className="rounded-2xl border bg-white p-6 shadow-sm lg:col-span-2">

                    <h2 className="mb-4 text-xl font-semibold">
                        Revenue Overview
                    </h2>

                    <div className="flex h-80 items-center justify-center rounded-xl border-2 border-dashed border-gray-300 bg-gray-50">

                        <div className="text-center">

                            <TrendingUp className="mx-auto mb-3 h-12 w-12 text-blue-600" />

                            <p className="text-lg font-semibold">
                                Revenue Analytics
                            </p>

                            <p className="text-gray-500">
                                Sales charts will appear here.
                            </p>

                        </div>

                    </div>

                </div>

                <div className="rounded-2xl border bg-white p-6 shadow-sm">

                    <h2 className="mb-5 text-xl font-semibold">
                        Recent Activities
                    </h2>

                    <div className="space-y-5">

                        <div className="flex gap-3">

                            <Clock className="mt-1 h-5 w-5 text-blue-600" />

                            <div>

                                <p className="font-medium">
                                    New customer registered
                                </p>

                                <span className="text-sm text-gray-500">
                                    15 minutes ago
                                </span>

                            </div>

                        </div>

                        <div className="flex gap-3">

                            <Clock className="mt-1 h-5 w-5 text-green-600" />

                            <div>

                                <p className="font-medium">
                                    Invoice paid
                                </p>

                                <span className="text-sm text-gray-500">
                                    Today
                                </span>

                            </div>

                        </div>

                        <div className="flex gap-3">

                            <Clock className="mt-1 h-5 w-5 text-purple-600" />

                            <div>

                                <p className="font-medium">
                                    New project created
                                </p>

                                <span className="text-sm text-gray-500">
                                    Yesterday
                                </span>

                            </div>

                        </div>

                    </div>

                </div>

            </div>

        </div>
    );
}