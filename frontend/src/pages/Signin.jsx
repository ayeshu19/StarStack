import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";

export default function Signin() {
  const navigate = useNavigate();
  const [role, setRole] = useState("DRIVER");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");

  async function handleLogin(e) {
    e.preventDefault();
    setError("");

    try {
      const response = await axios.post("http://localhost:5028/api/auth/login", {
        username,
        password
      });

      const { token, user } = response.data;

      // Block wrong role-type login
      if (role !== user.role) {
        setError("Can't login, please check your role");
        return;
      }

      // Save token & details
      localStorage.setItem("token", token);
      localStorage.setItem("userRole", user.role);
      localStorage.setItem("userId", user.id);
      localStorage.setItem("driverId", user.id);
      localStorage.setItem("driverData", JSON.stringify({
        name: user.name,
        email: user.email,
        role: user.role
      }));

      // Attach token to axios globally
      axios.defaults.headers.common["Authorization"] = `Bearer ${token}`;

      // Redirect based on actual backend user role
      if (user.role === "ADMIN") {
        navigate("/admin");
      } else if (user.role === "DRIVER") {
        navigate("/driver-dashboard");
      } else {
        setError("Unknown role returned by server.");
      }

    } catch (err) {
      setError("Invalid username or password");
    }
  }

  return (
    <div className="ss-page-root">
      <div className="ss-center-wrap">

        <header className="ss-header">
          <img
            src="src/assets/truck.png"
            alt="truck"
            className="ss-truck-inline"
          />
          <h1 className="ss-title-inline">ShiftSync</h1>
        </header>

        <div className="ss-tabs-wrap">
          <div className="ss-tabs" role="tablist">
            <button
              className={`ss-tab ${role === "DRIVER" ? "ss-tab-active" : ""}`}
              onClick={() => setRole("DRIVER")}
            >
              DRIVER
            </button>

            <button
              className={`ss-tab ${role === "ADMIN" ? "ss-tab-active" : ""}`}
              onClick={() => setRole("ADMIN")}
            >
              ADMIN
            </button>
          </div>
        </div>

        <form className="ss-form" onSubmit={handleLogin}>
          {error && <p style={{ color: "red" }}>{error}</p>}

          <div className="ss-field">
            <input
              className="ss-input"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder={
                role === "ADMIN" ? "Username or Email" : "Phone or Email"
              }
            />
          </div>

          <div className="ss-field ss-password-field">
            <input
              className="ss-input"
              type={showPassword ? "text" : "password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
            />

            <button
              type="button"
              className="ss-eye-btn"
              onClick={() => setShowPassword((s) => !s)}
            >
              {showPassword ? "🙈" : "👁️"}
            </button>
          </div>

          <label className="ss-remember">
            <input
              type="checkbox"
              checked={remember}
              onChange={(e) => setRemember(e.target.checked)}
            />
            <span>Remember Me</span>
          </label>

          <div className="ss-login-wrap">
            <button type="submit" className="ss-login-btn">
              LOGIN
            </button>
          </div>
        </form>

      </div>
    </div>
  );
}
