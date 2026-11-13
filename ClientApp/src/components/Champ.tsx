import type { ReactNode } from 'react'

// Attributs d'accessibilité d'un champ : erreur reliée par aria-describedby
export function aria(id: string, erreur?: string, aide?: boolean) {
  const decrit = [aide ? `${id}-aide` : null, erreur ? `${id}-erreur` : null].filter(Boolean).join(' ')
  return {
    id,
    'aria-invalid': erreur ? (true as const) : undefined,
    'aria-describedby': decrit || undefined,
  }
}

export function Champ(props: {
  id: string
  libelle: string
  erreur?: string
  aide?: string
  facultatif?: boolean
  obligatoire?: boolean
  children: ReactNode
}) {
  const { id, libelle, erreur, aide, facultatif, obligatoire, children } = props
  return (
    <div className="champ">
      <label htmlFor={id}>
        {libelle}
        {obligatoire && <span aria-hidden="true"> *</span>}
        {facultatif && <span className="facultatif"> (facultatif)</span>}
      </label>
      {aide && <p className="aide" id={`${id}-aide`}>{aide}</p>}
      {children}
      {erreur && (
        <p className="erreur-champ" id={`${id}-erreur`}>
          {erreur}
        </p>
      )}
    </div>
  )
}
