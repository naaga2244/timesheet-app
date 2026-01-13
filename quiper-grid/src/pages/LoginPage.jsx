import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../services/authService";
import companyLogo from "../assets/Timetric.png"

const LoginPage = () => {
    const [email, setEmail] = useState("");
    const [error, setError] = useState("");
    const navigate = useNavigate();

    const handleSubmit = async (e) => {
        e.preventDefault();
        const result = await login(email);

        if (!result.success) {
            setError(result.message || "Login failed");
            return;
        }

        const user = result.message;
        navigate(user.role === "admin" ? "/admin" : "/user");
    };

    return (
        <div className="container-fluid page p-0">
            <div className="logo">
                <img src={companyLogo} alt="Company Logo" />
            </div>
            <div className="form-box">
                <h5 className="emailid">Email ID</h5>
                <form onSubmit={handleSubmit}>
                    <input
                        type="email"
                        placeholder="Email"
                        value={email}
                        onChange={(e) => {
                            setEmail(e.target.value);
                            if(e.target.value.trim() === ""){
                                setError("");
                            }
                        }}
                        required
                        className="mb-3"
                    />
                    <button className="btn btn-dark text-white"
                        type="submit"
                        style={
                            {
                                width: "30%",
                                borderRadius: "0"
                            }
                        }
                    >
                        Sign In
                    </button>
                </form>
                {error && <p className="text-center text-danger mt-2">{error}</p>}
            </div>
        </div>
    );
};

export default LoginPage;