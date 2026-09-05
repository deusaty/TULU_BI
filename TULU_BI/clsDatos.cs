using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Newtonsoft.Json;

namespace TULU_BI
{
    public class clsClientes
    {
        public int id;
        public string nombre;
        public string telefono;
        public string direccion;
        public string correo;
    }

    public class clsDatos
    {
        // 1. QUITAMOS el punto y coma (;) al final de la firma del método
        public List<clsClientes> cargarClientes()
        {
            List<clsClientes> lista = new List<clsClientes>();

            // 2. Usamos el nombre correcto de tu servicio generado (asegúrate de que coincida con tu Service Reference)
            ServiceReference1.Service1Client ws = new ServiceReference1.Service1Client();

            // 3. Llamamos al método exactamente como viene en tu WS (CargaClientes)
            string res = ws.CargaClientes();
            ws.Close();

            DataTable dt = JsonConvert.DeserializeObject<DataTable>(res) ?? new DataTable();

            foreach (DataRow row in dt.Rows)
            {
                clsClientes c = new clsClientes();
                c.id = row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0;
                c.nombre = row["nombre"] != DBNull.Value ? Convert.ToString(row["nombre"]) : "";
                c.telefono = row["telefono"] != DBNull.Value ? Convert.ToString(row["telefono"]) : "";
                c.direccion = row["direccion"] != DBNull.Value ? Convert.ToString(row["direccion"]) : "";
                c.correo = row["correo"] != DBNull.Value ? Convert.ToString(row["correo"]) : "";

                lista.Add(c);
            }

            return lista;
        }
    }
}
