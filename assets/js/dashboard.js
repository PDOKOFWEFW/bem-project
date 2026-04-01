/**
 * EndpointApi — dashboard ve yönetim istekleri.
 * API: <meta name="endpoint-api-base" content="http://localhost:5000">
 * Admin (EndpointApi SecuritySettings:AdminApiKey ile aynı): <meta name="admin-api-key" content="">
 */
(function (global) {
  "use strict";

  var ADMIN_HEADER = "X-Admin-Api-Key";

  function getApiBase() {
    var meta = document.querySelector('meta[name="endpoint-api-base"]');
    if (meta && meta.content) return meta.content.replace(/\/$/, "");
    if (global.ENDPOINT_API_BASE) return String(global.ENDPOINT_API_BASE).replace(/\/$/, "");
    return "http://localhost:5000";
  }

  function getAdminHeaders() {
    var h = {};
    var meta = document.querySelector('meta[name="admin-api-key"]');
    var key = meta && meta.content ? String(meta.content).trim() : "";
    if (!key && global.ADMIN_API_KEY) key = String(global.ADMIN_API_KEY).trim();
    if (key) h[ADMIN_HEADER] = key;
    return h;
  }

  function apiFetch(path, options) {
    options = options || {};
    var mergedHeaders = Object.assign({}, getAdminHeaders(), options.headers || {});
    var next = Object.assign({}, options, { headers: mergedHeaders });
    return fetch(getApiBase() + path, next);
  }

  function fetchDashboardJson() {
    return apiFetch("/api/dashboard", { credentials: "omit" }).then(function (res) {
      if (!res.ok) return res.text().then(function (t) { throw new Error(t || res.statusText); });
      return res.json();
    });
  }

  function formatDateTime(iso) {
    if (!iso) return "—";
    try {
      var d = new Date(iso);
      return d.toLocaleString("tr-TR", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
    } catch (e) {
      return String(iso);
    }
  }

  function inferOs(osVersion) {
    if (!osVersion) return "Linux";
    var s = String(osVersion);
    if (/windows/i.test(s)) return "Windows";
    if (/darwin|mac\s*os/i.test(s)) return "macOS";
    if (/linux/i.test(s)) return "Linux";
    return "Linux";
  }

  function riskLabel(score) {
    if (score >= 60) return "Yüksek Risk";
    if (score >= 30) return "Orta Risk";
    return "Düşük Risk";
  }

  function normalizeDeviceRows(rows) {
    return (rows || []).map(function (d) {
      return {
        name: d.machineName || d.MachineName || "—",
        deviceId: d.deviceId || d.DeviceId || "",
        userName: d.userDisplayName || d.UserDisplayName || "—",
        userMail: d.userEmail || d.UserEmail || "",
        ip: d.ipAddress || d.IpAddress || "",
        os: inferOs(d.osVersion || d.OsVersion),
        browsers: { chrome: "—", edge: "—", firefox: "—" },
        risk: typeof d.riskScore === "number" ? d.riskScore : d.RiskScore || 0,
        status: d.riskLabel || d.RiskLabel || riskLabel(typeof d.riskScore === "number" ? d.riskScore : d.RiskScore || 0),
        ou: d.organizationUnitName || d.OrganizationUnitName || "",
        lastSync: formatDateTime((d.lastSeenUtc || d.LastSeenUtc))
      };
    });
  }

  function setText(id, text) {
    var el = document.getElementById(id);
    if (el) el.textContent = text;
  }

  var trendChartRef = null;
  var donutChartRef = null;

  function destroyChart(ref) {
    if (!ref) return;
    try {
      ref.destroy();
    } catch (e) {}
  }

  function renderOverviewCharts(data) {
    var trend = data.activityTrend || data.ActivityTrend || {};
    var dist = data.statusDistribution || data.StatusDistribution || {};
    var labels = trend.labels || trend.Labels || [];
    var reg = trend.registeredDevicesSeries || trend.RegisteredDevicesSeries || [];
    var act = trend.activeDevicesSeries || trend.ActiveDevicesSeries || [];

    var trendCtx = document.getElementById("trendChart");
    if (trendCtx && typeof Chart !== "undefined") {
      destroyChart(trendChartRef);
      trendChartRef = new Chart(trendCtx, {
        type: "line",
        data: {
          labels: labels,
          datasets: [
            {
              label: "Aktif cihaz (aylık son görülme)",
              data: act,
              borderColor: "#1a73e8",
              backgroundColor: "rgba(26, 115, 232, 0.15)",
              fill: true,
              tension: 0.35,
              borderWidth: 2.5,
              pointRadius: 0,
              pointHoverRadius: 4
            },
            {
              label: "Yeni kayıt (cihaz oluşturma)",
              data: reg,
              borderColor: "#34a853",
              backgroundColor: "rgba(52, 168, 83, 0.08)",
              fill: true,
              tension: 0.35,
              borderWidth: 2.2,
              pointRadius: 0,
              pointHoverRadius: 4
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          interaction: { mode: "index", intersect: false },
          plugins: {
            legend: {
              position: "top",
              align: "end",
              labels: {
                usePointStyle: true,
                boxWidth: 8,
                color: "#5f6368",
                font: { size: 12, family: "Inter" }
              }
            },
            tooltip: {
              backgroundColor: "#1f1f1f",
              titleColor: "#fff",
              bodyColor: "#fff",
              padding: 10
            }
          },
          scales: {
            x: {
              grid: { display: false },
              ticks: { color: "#5f6368", font: { size: 11, family: "Inter" } }
            },
            y: {
              beginAtZero: true,
              grid: { color: "#edf0f2" },
              ticks: { color: "#5f6368", font: { size: 11, family: "Inter" } }
            }
          }
        }
      });
    }

    var dLabels = dist.labels || dist.Labels || [];
    var dVals = dist.values || dist.Values || [];
    var colors = ["#1a73e8", "#34a853", "#f9ab00", "#ea4335", "#5f6368"];
    var totalDist = dVals.reduce(function (a, b) { return a + b; }, 0);

    var donutCtx = document.getElementById("donutChart");
    var legendRoot = document.getElementById("donutLegend");
    if (legendRoot) legendRoot.innerHTML = "";

    if (donutCtx && typeof Chart !== "undefined") {
      destroyChart(donutChartRef);
      donutChartRef = new Chart(donutCtx, {
        type: "doughnut",
        data: {
          labels: dLabels,
          datasets: [
            {
              data: dVals,
              backgroundColor: dLabels.map(function (_, i) { return colors[i % colors.length]; }),
              borderWidth: 0,
              hoverOffset: 6
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          cutout: "68%",
          plugins: {
            legend: { display: false },
            tooltip: {
              callbacks: {
                label: function (ctx) {
                  var v = ctx.raw;
                  var pct = totalDist ? Math.round((v / totalDist) * 100) : 0;
                  return ctx.label + ": " + v + " (" + pct + "%)";
                }
              }
            }
          }
        }
      });
    }

    if (legendRoot && dLabels.length) {
      dLabels.forEach(function (label, i) {
        var color = colors[i % colors.length];
        var value = dVals[i] || 0;
        var pct = totalDist ? Math.round((value / totalDist) * 100) : 0;
        var row = document.createElement("div");
        row.className = "legend-item";
        row.innerHTML =
          '<div class="legend-left"><span class="swatch" style="background:' +
          color +
          '"></span><span>' +
          label +
          '</span></div><strong>' +
          value +
          " (" +
          pct +
          "%)</strong>";
        legendRoot.appendChild(row);
      });
    }
  }

  function renderRecentEvents(events) {
    var tbody = document.getElementById("recent-events-body");
    if (!tbody) return;
    tbody.innerHTML = "";
    (events || []).forEach(function (ev) {
      var title = ev.title || ev.Title || "";
      var source = ev.source || ev.Source || "";
      var when = formatDateTime(ev.occurredUtc || ev.OccurredUtc);
      var sev = (ev.severity || ev.Severity || "Bilgi").toLowerCase();
      var statusClass = "ok";
      if (sev.indexOf("kritik") >= 0 || sev.indexOf("risk") >= 0) statusClass = "risk";
      else if (sev.indexOf("uyar") >= 0 || sev.indexOf("warn") >= 0) statusClass = "warn";
      var tr = document.createElement("tr");
      tr.innerHTML =
        "<td>" +
        escapeHtml(title) +
        "</td><td>" +
        escapeHtml(source) +
        "</td><td>" +
        escapeHtml(when) +
        '</td><td><span class="status ' +
        statusClass +
        '">' +
        escapeHtml(ev.severity || ev.Severity || "Bilgi") +
        "</span></td>";
      tbody.appendChild(tr);
    });
  }

  function escapeHtml(s) {
    return String(s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function bindOverviewKpis(stats) {
    var s = stats || {};
    var total = s.totalDevices != null ? s.totalDevices : s.TotalDevices;
    var risky = s.riskyDevices != null ? s.riskyDevices : s.RiskyDevices;
    var signals = s.openSecuritySignals != null ? s.openSecuritySignals : s.OpenSecuritySignals;
    var comp = s.complianceScorePercent != null ? s.complianceScorePercent : s.ComplianceScorePercent;
    var last = s.lastSyncUtc || s.LastSyncUtc;

    setText("kpi-total-devices", formatInt(total));
    setText("kpi-risky-devices", formatInt(risky));
    setText("kpi-open-signals", formatInt(signals));
    setText("kpi-compliance", comp != null ? String(comp) + "%" : "—");

    setText("kpi-total-devices-sub", "Kayıtlı uç nokta");
    setText("kpi-risky-sub", "Risk skoru ≥ 60");
    setText("kpi-signals-sub", "Riskli cihaz özeti");
    setText("kpi-compliance-sub", "Tahmini uyumluluk");

    if (last) {
      setText("sidebar-sync-line", "Son senkronizasyon: " + formatDateTime(last));
    }
    var r = risky != null ? Number(risky) : 0;
    setText(
      "sidebar-sync-hint",
      r > 0 ? r + " cihaz yüksek risk altında (≥60)." : "Şu an yüksek riskli cihaz yok."
    );
  }

  function formatInt(n) {
    if (n == null || isNaN(n)) return "0";
    return Number(n).toLocaleString("tr-TR");
  }

  function initOverview() {
    return fetchDashboardJson()
      .then(function (data) {
        var stats = data.stats || data.Stats;
        bindOverviewKpis(stats);
        renderOverviewCharts(data);
        var events = data.recentEvents || data.RecentEvents;
        renderRecentEvents(events);
        setText("topbar-date", new Date().toLocaleDateString("tr-TR", { weekday: "long", year: "numeric", month: "long", day: "numeric" }));
      })
      .catch(function (err) {
        console.error(err);
        setText("kpi-total-devices", "—");
        setText("sidebar-sync-line", "API bağlantı hatası: " + (err.message || err));
      });
  }

  function getRiskClass(score) {
    if (score >= 60) return "high";
    if (score >= 30) return "mid";
    return "low";
  }

  function getOsClass(os) {
    if (os === "Windows") return "os-win";
    if (os === "macOS") return "os-mac";
    return "os-linux";
  }

  function postMoveOu(deviceId, targetOuName) {
    var body = JSON.stringify({
      deviceId: deviceId,
      targetOrganizationUnitName: targetOuName
    });
    return apiFetch("/api/device/move-ou", {
      method: "POST",
      credentials: "omit",
      headers: { "Content-Type": "application/json" },
      body: body
    }).then(function (res) {
      if (res.status === 401) throw new Error("Admin API anahtarı geçersiz veya eksik (meta admin-api-key / SecuritySettings:AdminApiKey).");
      if (!res.ok) return res.text().then(function (t) { throw new Error(t || res.statusText); });
    });
  }

  function initDevices() {
    var tableBody = document.getElementById("deviceTableBody");
    var searchInput = document.getElementById("searchInput");
    var osFilter = document.getElementById("osFilter");
    var statusFilter = document.getElementById("statusFilter");
    var clearFiltersBtn = document.getElementById("clearFiltersBtn");
    var resultMeta = document.getElementById("resultMeta");
    var emptyState = document.getElementById("emptyState");

    var devices = [];

    function renderRows(list) {
      if (!tableBody) return;
      tableBody.innerHTML = "";
      list.forEach(function (item) {
        var mailLine = item.userMail ? item.userMail : item.ip ? item.ip : "";
        var tr = document.createElement("tr");
        tr.innerHTML =
          "<td><strong>" +
          escapeHtml(item.name) +
          "</strong><div class=\"mail\" style=\"font-size:11px;color:var(--muted)\">" +
          escapeHtml(item.deviceId) +
          "</div></td>" +
          '<td class="user-col"><div class="name">' +
          escapeHtml(item.userName) +
          "</div><div class=\"mail\">" +
          escapeHtml(mailLine) +
          "</div>" +
          (item.ou
            ? '<div class="mail" style="margin-top:4px">OU: ' + escapeHtml(item.ou) + "</div>"
            : "") +
          "</td>" +
          '<td><span class="os-pill ' +
          getOsClass(item.os) +
          '">' +
          escapeHtml(item.os) +
          "</span></td>" +
          '<td><div class="browser-badges">' +
          '<span class="browser-badge">Chrome ' +
          escapeHtml(item.browsers.chrome) +
          "</span>" +
          '<span class="browser-badge">Edge ' +
          escapeHtml(item.browsers.edge) +
          "</span>" +
          '<span class="browser-badge">Firefox ' +
          escapeHtml(item.browsers.firefox) +
          "</span></div></td>" +
          '<td><span class="risk ' +
          getRiskClass(item.risk) +
          '">' +
          item.risk +
          "</span> / 100</td>" +
          '<td class="sync">' +
          escapeHtml(item.lastSync) +
          '</td><td><div class="row-actions">' +
          '<button class="action-btn" type="button">Detay</button>' +
          '<button class="action-btn move-ou-btn" type="button" data-device-id="' +
          escapeHtml(item.deviceId) +
          '">Taşı</button></div></td>';
        tableBody.appendChild(tr);
      });
      if (resultMeta) resultMeta.textContent = "Toplam " + list.length + " cihaz gösteriliyor";
      if (emptyState) emptyState.style.display = list.length === 0 ? "block" : "none";
    }

    function applyFilters() {
      var searchText = (searchInput && searchInput.value ? searchInput.value : "").toLowerCase().trim();
      var osText = osFilter ? osFilter.value : "";
      var statusText = statusFilter ? statusFilter.value : "";
      var filtered = devices.filter(function (item) {
        var searchable =
          (item.name + " " + item.userName + " " + item.userMail + " " + item.os + " " + item.status + " " + item.ou + " " + item.deviceId).toLowerCase();
        var matchesSearch = !searchText || searchable.indexOf(searchText) >= 0;
        var matchesOs = !osText || item.os === osText;
        var matchesStatus = !statusText || item.status === statusText;
        return matchesSearch && matchesOs && matchesStatus;
      });
      renderRows(filtered);
    }

    function applyDevicesPageSideEffects(data) {
      var stats = data.stats || data.Stats;
      var last = stats && (stats.lastSyncUtc || stats.LastSyncUtc);
      if (last) {
        var el = document.querySelector(".sidebar-footer strong");
        if (el) el.textContent = formatDateTime(last);
      }
      var hintEl = document.getElementById("devices-sidebar-hint");
      if (hintEl && stats) {
        var t = stats.totalDevices != null ? stats.totalDevices : stats.TotalDevices;
        var r = stats.riskyDevices != null ? stats.riskyDevices : stats.RiskyDevices;
        hintEl.textContent = "Toplam " + formatInt(t) + " cihaz; " + formatInt(r) + " yüksek riskli.";
      }
      setText("topbar-date-devices", new Date().toLocaleDateString("tr-TR", { weekday: "long", year: "numeric", month: "long", day: "numeric" }));
    }

    function loadDevicesFromApi() {
      return fetchDashboardJson().then(function (data) {
        devices = normalizeDeviceRows(data.devices || data.Devices);
        applyFilters();
        applyDevicesPageSideEffects(data);
      });
    }

    if (tableBody && !tableBody.dataset.moveDelegateBound) {
      tableBody.dataset.moveDelegateBound = "1";
      tableBody.addEventListener("click", function (ev) {
        var btn = ev.target && ev.target.closest ? ev.target.closest(".move-ou-btn") : null;
        if (!btn) return;
        var did = btn.getAttribute("data-device-id");
        if (!did) return;
        var target = window.prompt("Hedef OU adı (örn. Pazarlama, Unassigned, Default):", "Pazarlama");
        if (target === null || !String(target).trim()) return;
        btn.disabled = true;
        postMoveOu(did, String(target).trim())
          .then(function () {
            return loadDevicesFromApi();
          })
          .then(function () {
            window.alert("Cihaz OU güncellendi.");
          })
          .catch(function (err) {
            console.error(err);
            window.alert("Taşıma başarısız: " + (err.message || err));
          })
          .finally(function () {
            btn.disabled = false;
          });
      });
    }

    return loadDevicesFromApi()
      .then(function () {
        if (searchInput) searchInput.addEventListener("input", applyFilters);
        if (osFilter) osFilter.addEventListener("change", applyFilters);
        if (statusFilter) statusFilter.addEventListener("change", applyFilters);
        if (clearFiltersBtn)
          clearFiltersBtn.addEventListener("click", function () {
            if (searchInput) searchInput.value = "";
            if (osFilter) osFilter.value = "";
            if (statusFilter) statusFilter.value = "";
            applyFilters();
          });
      })
      .catch(function (err) {
        console.error(err);
        if (resultMeta) resultMeta.textContent = "API yüklenemedi: " + (err.message || err);
        renderRows([]);
      });
  }

  global.DashboardApp = {
    initOverview: initOverview,
    initDevices: initDevices,
    getApiBase: getApiBase,
    getAdminHeaders: getAdminHeaders,
    apiFetch: apiFetch
  };
})(typeof window !== "undefined" ? window : this);
