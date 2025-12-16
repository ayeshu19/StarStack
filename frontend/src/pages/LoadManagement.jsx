import React, { useEffect, useMemo, useState } from "react";
import { FiPlus, FiChevronDown, FiX, FiUpload } from "react-icons/fi";

// =================== API CONFIG =================== //
const API_BASE = "http://localhost:5028/api";

// =================== BADGE COMPONENTS =================== //
function StatusBadge({ label, tone }) {
  return <span className={`badge badge-status badge-status-${tone}`}>{label}</span>;
}

function PriorityBadge({ label }) {
  const tone = label === "HIGH" ? "high" : label === "MEDIUM" ? "medium" : "low";
  const pretty = label === "HIGH" ? "High" : label === "MEDIUM" ? "Medium" : "Low";
  return <span className={`badge badge-pill badge-${tone}`}>{pretty}</span>;
}

// =================== FILTER DROPDOWN =================== //
function FilterDropdown({ label, value, options, onChange }) {
  return (
    <div className="filter-dropdown">
      <span className="filter-label">{label}</span>

      <div className="filter-control" style={{ position: "relative" }}>
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          style={{
            appearance: "none",
            WebkitAppearance: "none",
            MozAppearance: "none",
            background: "transparent",
            border: "none",
            width: "100%",
            paddingRight: "28px",
            cursor: "pointer",
            font: "inherit",
            color: "inherit",
            outline: "none",
          }}
        >
          {options.map((opt) => (
            <option key={opt.value ?? opt} value={opt.value ?? opt}>
              {opt.label ?? opt}
            </option>
          ))}
        </select>

        <FiChevronDown
          className="filter-icon"
          style={{ position: "absolute", right: 8, top: "50%", transform: "translateY(-50%)" }}
        />
      </div>
    </div>
  );
}

// =================== PAGE HEADER =================== //
function PageHeader({ title, subtitle, rightSlot }) {
  return (
    <header className="page-header">
      <div>
        <h1 className="page-title">{title}</h1>
        {subtitle && <p className="page-subtitle">{subtitle}</p>}
      </div>
      {rightSlot && <div className="page-header-right">{rightSlot}</div>}
    </header>
  );
}

// =================== MODAL =================== //
function Modal({ title, children, onClose }) {
  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.35)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 9999,
        padding: 16,
      }}
      onClick={onClose}
    >
      <div
        className="card"
        style={{ width: "min(560px, 100%)", padding: 16 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 12 }}>
          <h3 style={{ margin: 0 }}>{title}</h3>
          <button className="action-link" onClick={onClose} style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <FiX /> Close
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}

// =================== MAIN COMPONENT =================== //
function LoadManagement() {
  // Filters - Default to "All" to show all loads
  const [dateFilter, setDateFilter] = useState("All");
  const [regionFilter, setRegionFilter] = useState("All Regions");
  const [statusFilter, setStatusFilter] = useState("All Statuses");

  // Data
  const [loads, setLoads] = useState([]);
  const [stats, setStats] = useState(null);

  // UI states
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Modals
  const [showAdd, setShowAdd] = useState(false);
  const [showView, setShowView] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [selectedLoad, setSelectedLoad] = useState(null);

  // Upload modal states ✅ (MUST be inside component)
  const [showUpload, setShowUpload] = useState(false);
  const [uploadFile, setUploadFile] = useState(null);

  // Form (Add/Edit)
  const [form, setForm] = useState({
    region: "",
    stops: "",
    estimatedHours: "",
    estimatedDistance: "",
    priority: "MEDIUM",
  });

  // Dropdown options
  const dateOptions = useMemo(
    () => [
      { label: "Today", value: "Today" },
      { label: "All Dates", value: "All" },
    ],
    []
  );

  // ✅ Dynamic region options (unique areas from table)
  const regionOptions = useMemo(() => {
    const uniqueAreas = Array.from(
      new Set(
        loads
          .map((l) => (l.area ?? "").trim())
          .filter((v) => v.length > 0)
      )
    ).sort((a, b) => a.localeCompare(b));

    return [
      { label: "All Regions", value: "All Regions" },
      ...uniqueAreas.map((area) => ({ label: area, value: area })),
    ];
  }, [loads]);

  useEffect(() => {
    if (regionFilter !== "All Regions") {
      const exists = regionOptions.some((o) => o.value === regionFilter);
      if (!exists) setRegionFilter("All Regions");
    }
  }, [regionOptions, regionFilter]);

  const statusOptions = useMemo(
    () => [
      { label: "All Statuses", value: "All Statuses" },
      { label: "Pending", value: "PENDING" },
      { label: "Assigned", value: "ASSIGNED" },
      { label: "In Progress", value: "IN_PROGRESS" },
      { label: "Completed", value: "COMPLETED" },
    ],
    []
  );

  // ---------- Helpers ----------
  const buildQueryParams = () => {
    const params = new URLSearchParams();

    if (regionFilter !== "All Regions") params.set("region", regionFilter);
    if (statusFilter !== "All Statuses") params.set("status", statusFilter);

    if (dateFilter === "Today") {
      const today = new Date();
      const yyyy = today.getFullYear();
      const mm = String(today.getMonth() + 1).padStart(2, "0");
      const dd = String(today.getDate()).padStart(2, "0");
      params.set("date", `${yyyy}-${mm}-${dd}`);
    }

    return params.toString();
  };

  const mapRowForUI = (l) => {
    const statusTone =
      l.status === "PENDING"
        ? "pending"
        : l.status === "ASSIGNED"
        ? "assigned"
        : l.status === "IN_PROGRESS"
        ? "progress"
        : "completed";

    const statusLabel =
      l.status === "PENDING"
        ? "Pending"
        : l.status === "ASSIGNED"
        ? "Assigned"
        : l.status === "IN_PROGRESS"
        ? "In Progress"
        : "Completed";

    return {
      id: l.loadRef ?? l.loadId,
      loadId: l.loadId,
      loadRef: l.loadRef,
      region: l.region,
      packages: l.stops,
      distance: `${Number(l.estimatedDistance).toFixed(2)} km`,
      area: l.region,
      priority: l.priority,
      status: { label: statusLabel, tone: statusTone },
      assignedDriverName: l.assignedDriverName ?? "-",
      estimatedHours: l.estimatedHours,
      estimatedDistance: l.estimatedDistance,
      stops: l.stops,
      createdAt: l.createdAt,
    };
  };

  // ---------- API calls ----------
  const fetchLoadsAndStats = async () => {
    setLoading(true);
    setError("");

    try {
      const query = buildQueryParams();

      const [loadsRes, statsRes] = await Promise.all([
        fetch(`${API_BASE}/Loads${query ? `?${query}` : ""}`),
        fetch(`${API_BASE}/Loads/stats?${query}`)
,
      ]);

      if (!loadsRes.ok) throw new Error((await loadsRes.text()) || "Failed to load loads");
      if (!statsRes.ok) throw new Error((await statsRes.text()) || "Failed to load stats");

      const loadsData = await loadsRes.json();
      const statsData = await statsRes.json();

      setLoads(Array.isArray(loadsData) ? loadsData.map(mapRowForUI) : []);
      setStats(statsData);
    } catch (e) {
      console.error(e);
      setError(e.message || "Something went wrong");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLoadsAndStats();
    // eslint-disable-next-line
  }, [dateFilter, regionFilter, statusFilter]);

  // ---------- Actions ----------
  const openAdd = () => {
    setForm({
      region: "",
      stops: "",
      estimatedHours: "",
      estimatedDistance: "",
      priority: "MEDIUM",
    });
    setShowAdd(true);
  };

  const onCreateLoad = async () => {
    setError("");
    try {
      if (!form.region.trim()) throw new Error("Region is required");
      if (Number(form.stops) <= 0) throw new Error("Packages/Stops must be > 0");
      if (Number(form.estimatedHours) <= 0) throw new Error("Estimated Hours must be > 0");
      if (Number(form.estimatedDistance) <= 0) throw new Error("Distance must be > 0");

      const res = await fetch(`${API_BASE}/Loads`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          region: form.region.trim(),
          stops: Number(form.stops),
          estimatedHours: Number(form.estimatedHours),
          estimatedDistance: Number(form.estimatedDistance),
          priority: form.priority,
        }),
      });

      if (!res.ok) throw new Error((await res.text()) || "Create failed");

      setShowAdd(false);
      await fetchLoadsAndStats();
    } catch (e) {
      console.error(e);
      setError(e.message || "Create failed");
    }
  };

  const onView = (row) => {
    setSelectedLoad(row);
    setShowView(true);
  };

  const onEditOpen = (row) => {
    setSelectedLoad(row);
    setForm({
      region: row.region ?? "",
      stops: row.stops,
      estimatedHours: row.estimatedHours,
      estimatedDistance: row.estimatedDistance,
      priority: row.priority ?? "MEDIUM",
    });
    setShowEdit(true);
  };

  const onEditSave = async () => {
    setError("");
    try {
      if (!selectedLoad) return;
      if (!form.region.trim()) throw new Error("Region is required");

      const res = await fetch(`${API_BASE}/Loads/${selectedLoad.loadId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          region: form.region.trim(),
          stops: Number(form.stops),
          estimatedHours: Number(form.estimatedHours),
          estimatedDistance: Number(form.estimatedDistance),
          priority: form.priority,
          status: "PENDING",
        }),
      });

      if (!res.ok) throw new Error((await res.text()) || "Update failed");

      setShowEdit(false);
      setSelectedLoad(null);
      await fetchLoadsAndStats();
    } catch (e) {
      console.error(e);
      setError(e.message || "Update failed");
    }
  };

  const onDelete = async (row) => {
    setError("");
    const ok = window.confirm(`Delete load ${row.loadRef || row.id}?`);
    if (!ok) return;

    try {
      const res = await fetch(`${API_BASE}/Loads/${row.loadId}`, { method: "DELETE" });
      if (!res.ok) throw new Error((await res.text()) || "Delete failed");
      await fetchLoadsAndStats();
    } catch (e) {
      console.error(e);
      setError(e.message || "Delete failed");
    }
  };

  // =================== CSV UPLOAD =================== //
  const parseCSV = async (file) => {
    const text = await file.text();

    const lines = text
      .split(/\r?\n/)
      .map((l) => l.trim())
      .filter(Boolean);

    if (lines.length < 2) throw new Error("CSV must have header + at least 1 row");

    const headers = lines[0].split(",").map((h) => h.trim().toLowerCase());
    const idx = (name) => headers.indexOf(name.toLowerCase());

    const iRegion = idx("region");
    const iStops = idx("stops");
    const iDist = idx("estimatedDistance");
    const iPriority = idx("priority");
    const iHours = idx("estimatedHours"); // optional

    // handle possible camelcase vs lowercase in header
    const iDist2 = iDist === -1 ? idx("estimateddistance") : iDist;
    const iHours2 = iHours === -1 ? idx("estimatedhours") : iHours;

    if (iRegion === -1 || iStops === -1 || iDist2 === -1 || iPriority === -1) {
      throw new Error("CSV header must include: region,stops,estimatedDistance,priority");
    }

    const AVG_SPEED = 20; // Chennai traffic avg speed

    const rows = lines.slice(1).map((line) => {
      const cols = line.split(",").map((c) => c.trim());

      const region = cols[iRegion];
      const stops = Number(cols[iStops]);
      const estimatedDistance = Number(cols[iDist2]);
      const priority = (cols[iPriority] || "MEDIUM").toUpperCase();

      const estimatedHours =
        iHours2 !== -1 && cols[iHours2]
          ? Number(cols[iHours2])
          : Number((estimatedDistance / AVG_SPEED).toFixed(1));

      return { region, stops, estimatedDistance, estimatedHours, priority };
    });

    return rows.filter((r) => r.region && r.stops > 0 && r.estimatedDistance > 0);
  };

  const uploadLoads = async () => {
    try {
      setError("");
      if (!uploadFile) throw new Error("Please select a CSV file");

      const loadsToUpload = await parseCSV(uploadFile);
      if (loadsToUpload.length === 0) throw new Error("No valid rows found in CSV");

      const res = await fetch(`${API_BASE}/Loads/bulk`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(loadsToUpload),
      });

      if (!res.ok) throw new Error((await res.text()) || "Bulk upload failed");

      setShowUpload(false);
      setUploadFile(null);
      await fetchLoadsAndStats();
    } catch (e) {
      setError(e.message || "Bulk upload failed");
    }
  };

  // =================== RENDER =================== //
  return (
    <>
      <PageHeader
        title="Load Management"
        subtitle="Track and manage all shipment loads"
        rightSlot={
          <div style={{ display: "flex", gap: 10 }}>
            <button className="btn-primary" onClick={openAdd}>
              <FiPlus className="btn-icon" />
              Add Load
            </button>

            <button className="btn-primary" onClick={() => setShowUpload(true)} style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <FiUpload />
              Upload Loads
            </button>
          </div>
        }
      />

      {error && (
        <section className="card" style={{ padding: 12, borderLeft: "4px solid #ef4444" }}>
          <div style={{ color: "#b91c1c", fontWeight: 600 }}>Error</div>
          <div style={{ color: "#7f1d1d" }}>{error}</div>
        </section>
      )}

      <section className="card filters-card">
        <div className="filters-grid">
          <FilterDropdown label="Date" value={dateFilter} options={dateOptions} onChange={setDateFilter} />
          <FilterDropdown label="Region" value={regionFilter} options={regionOptions} onChange={setRegionFilter} />
          <FilterDropdown label="Status" value={statusFilter} options={statusOptions} onChange={setStatusFilter} />
        </div>
      </section>

      {stats && (
        <section className="card" style={{ padding: 16, marginBottom: 16 }}>
          <div style={{ display: "flex", gap: 12, flexWrap: "wrap" }}>
            <StatCard title="Total Loads" value={stats.totalLoads} />
            <StatCard title="Pending" value={stats.pendingLoads} />
            <StatCard title="Assigned" value={stats.assignedLoads} />
            <StatCard title="High Priority Pending" value={stats.highPriorityPending} />
          </div>
        </section>
      )}

      <section className="card panel">
        <div className="table-responsive">
          <table className="table">
            <thead>
              <tr>
                <th>Load ID</th>
                <th>Packages</th>
                <th>Distance</th>
                <th>Area</th>
                <th>Priority</th>
                <th>Status</th>
                <th style={{ minWidth: 220 }}>Actions</th>
              </tr>
            </thead>

            <tbody>
              {loading ? (
                <tr>
                  <td colSpan="7" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                    Loading...
                  </td>
                </tr>
              ) : loads.length === 0 ? (
                <tr>
                  <td colSpan="7" style={{ textAlign: "center", padding: "32px", color: "#6b7280" }}>
                    No loads available
                  </td>
                </tr>
              ) : (
                loads.map((load) => {
                  const canEditDelete = load?.status?.label === "Pending";
                  return (
                    <tr key={load.loadId}>
                      <td style={{ fontWeight: 500 }}>{load.loadRef || load.id}</td>
                      <td>{load.packages}</td>
                      <td>{load.distance}</td>
                      <td>{load.area}</td>
                      <td><PriorityBadge label={load.priority} /></td>
                      <td><StatusBadge label={load.status.label} tone={load.status.tone} /></td>
                      <td style={{ display: "flex", gap: 10, flexWrap: "wrap" }}>
                        <button className="action-link" onClick={() => onView(load)}>View Details</button>
                        <button className="action-link" disabled={!canEditDelete} onClick={() => onEditOpen(load)}>Edit</button>
                        <button className="action-link" disabled={!canEditDelete} onClick={() => onDelete(load)}>Delete</button>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </section>

      {/* ADD MODAL */}
      {showAdd && (
        <Modal title="Add Load" onClose={() => setShowAdd(false)}>
          <LoadForm form={form} setForm={setForm} />
          <div style={{ display: "flex", justifyContent: "flex-end", gap: 10, marginTop: 12 }}>
            <button className="action-link" onClick={() => setShowAdd(false)}>Cancel</button>
            <button className="btn-primary" onClick={onCreateLoad}>Create</button>
          </div>
        </Modal>
      )}

      {/* VIEW MODAL */}
      {showView && selectedLoad && (
        <Modal title={`Load Details - ${selectedLoad.loadRef || selectedLoad.id}`} onClose={() => setShowView(false)}>
          <div style={{ display: "grid", gap: 10 }}>
            <DetailRow label="Region" value={selectedLoad.region} />
            <DetailRow label="Stops/Packages" value={selectedLoad.stops} />
            <DetailRow label="Estimated Hours" value={selectedLoad.estimatedHours} />
            <DetailRow label="Estimated Distance" value={`${Number(selectedLoad.estimatedDistance).toFixed(2)} km`} />
            <DetailRow label="Priority" value={selectedLoad.priority} />
            <DetailRow label="Status" value={selectedLoad.status.label} />
            <DetailRow label="Assigned Driver" value={selectedLoad.assignedDriverName} />
          </div>
        </Modal>
      )}

      {/* EDIT MODAL */}
      {showEdit && selectedLoad && (
        <Modal title={`Edit Load - ${selectedLoad.loadRef || selectedLoad.id}`} onClose={() => setShowEdit(false)}>
          <LoadForm form={form} setForm={setForm} />
          <div style={{ display: "flex", justifyContent: "flex-end", gap: 10, marginTop: 12 }}>
            <button className="action-link" onClick={() => setShowEdit(false)}>Cancel</button>
            <button className="btn-primary" onClick={onEditSave}>Save</button>
          </div>
        </Modal>
      )}

      {/* ✅ UPLOAD MODAL */}
      {showUpload && (
        <Modal title="Upload Loads (CSV)" onClose={() => setShowUpload(false)}>
          <div style={{ display: "grid", gap: 10 }}>
            <input
              type="file"
              accept=".csv"
              onChange={(e) => setUploadFile(e.target.files?.[0] || null)}
            />

            <div style={{ fontSize: 12, color: "#6b7280" }}>
              CSV required columns: <b>region, stops, estimatedDistance, priority</b><br />
              Optional: <b>estimatedHours</b> (if missing → auto-calculated using Chennai traffic)
            </div>

            <div style={{ display: "flex", justifyContent: "flex-end", gap: 10 }}>
              <button className="action-link" onClick={() => setShowUpload(false)}>Cancel</button>
              <button className="btn-primary" onClick={uploadLoads}>Upload</button>
            </div>
          </div>
        </Modal>
      )}
    </>
  );
}

// =================== SMALL COMPONENTS =================== //
function StatCard({ title, value }) {
  return (
    <div style={{ flex: "1 1 160px", border: "1px solid #e5e7eb", borderRadius: 12, padding: 12 }}>
      <div style={{ fontSize: 12, color: "#6b7280" }}>{title}</div>
      <div style={{ fontSize: 22, fontWeight: 700 }}>{value ?? 0}</div>
    </div>
  );
}

function LoadForm({ form, setForm }) {
  return (
    <div style={{ display: "grid", gap: 12 }}>
      <div style={{ display: "grid", gap: 6 }}>
        <label style={{ fontSize: 12, color: "#6b7280" }}>Region</label>
        <input
          type="text"
          value={form.region}
          onChange={(e) => setForm((f) => ({ ...f, region: e.target.value }))}
          placeholder="Enter region (ex: Porur / Anna Nagar / Tambaram)"
          style={{ padding: 10, borderRadius: 10, border: "1px solid #e5e7eb" }}
        />
      </div>

      <div style={{ display: "grid", gap: 6 }}>
        <label style={{ fontSize: 12, color: "#6b7280" }}>Packages (Stops)</label>
        <input
          type="number"
          min="1"
          value={form.stops}
          onChange={(e) => setForm((f) => ({ ...f, stops: e.target.value }))}
          style={{ padding: 10, borderRadius: 10, border: "1px solid #e5e7eb" }}
        />
      </div>

      <div style={{ display: "grid", gap: 6 }}>
        <label style={{ fontSize: 12, color: "#6b7280" }}>Estimated Distance (km)</label>
        <input
          type="number"
          min="0.1"
          step="0.1"
          value={form.estimatedDistance}
          onChange={(e) => {
            const distance = e.target.value;
            const AVG_SPEED_KMH = 20;
            const hrs = distance && Number(distance) > 0 ? (Number(distance) / AVG_SPEED_KMH).toFixed(1) : "";
            setForm((f) => ({ ...f, estimatedDistance: distance, estimatedHours: hrs }));
          }}
          placeholder="Enter distance in km"
          style={{ padding: 10, borderRadius: 10, border: "1px solid #e5e7eb" }}
        />
      </div>

      <div style={{ display: "grid", gap: 6 }}>
        <label style={{ fontSize: 12, color: "#6b7280" }}>Estimated Hours (Auto)</label>
        <input
          type="text"
          value={form.estimatedHours}
          readOnly
          placeholder="Auto calculated based on distance"
          style={{
            padding: 10,
            borderRadius: 10,
            border: "1px solid #e5e7eb",
            background: "#f9fafb",
            cursor: "not-allowed",
          }}
        />
      </div>

      <div style={{ display: "grid", gap: 6 }}>
        <label style={{ fontSize: 12, color: "#6b7280" }}>Priority</label>
        <select
          value={form.priority}
          onChange={(e) => setForm((f) => ({ ...f, priority: e.target.value }))}
          style={{ padding: 10, borderRadius: 10, border: "1px solid #e5e7eb" }}
        >
          <option value="HIGH">High</option>
          <option value="MEDIUM">Medium</option>
          <option value="LOW">Low</option>
        </select>
      </div>
    </div>
  );
}

function DetailRow({ label, value }) {
  return (
    <div style={{ display: "flex", justifyContent: "space-between", gap: 12 }}>
      <div style={{ color: "#6b7280" }}>{label}</div>
      <div style={{ fontWeight: 600 }}>{value}</div>
    </div>
  );
}

export default LoadManagement;
